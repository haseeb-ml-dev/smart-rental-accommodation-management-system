using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.Services;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PaymentSlipStorage _proofStorage;

        public SubscriptionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, PaymentSlipStorage proofStorage)
        {
            _context = context;
            _userManager = userManager;
            _proofStorage = proofStorage;
        }

        [Authorize(Roles = "Landlord")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var landlord = await _userManager.GetUserAsync(User);
            if (landlord == null)
            {
                return NotFound();
            }

            var settings = await _context.AppSettings.FirstOrDefaultAsync();
            var today = DateTime.UtcNow;

            var vm = new SubscriptionStatusViewModel
            {
                IsTrialActive = landlord.TrialEndsAt.HasValue && landlord.TrialEndsAt.Value > today,
                IsSubscriptionActive = landlord.SubscriptionActiveUntil.HasValue && landlord.SubscriptionActiveUntil.Value > today,
                TrialEndsAt = landlord.TrialEndsAt,
                SubscriptionActiveUntil = landlord.SubscriptionActiveUntil,
                MonthlyFee = settings?.MonthlySubscriptionFee ?? 0m,
                PaymentInstructions = settings?.SubscriptionPaymentInstructions,
                PendingClaim = await _context.SubscriptionClaims
                    .Where(c => c.LandlordId == landlord.Id && c.Status == SubscriptionClaimStatus.Pending)
                    .OrderByDescending(c => c.ClaimedAt)
                    .FirstOrDefaultAsync(),
                History = await _context.SubscriptionClaims
                    .Where(c => c.LandlordId == landlord.Id)
                    .OrderByDescending(c => c.ClaimedAt)
                    .Take(10)
                    .ToListAsync()
            };

            return View(vm);
        }

        [Authorize(Roles = "Landlord")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(PaymentSlipStorage.MaxFileSizeBytes)]
        public async Task<IActionResult> Claim(IFormFile? proof)
        {
            var landlordId = _userManager.GetUserId(User)!;

            var alreadyPending = await _context.SubscriptionClaims
                .AnyAsync(c => c.LandlordId == landlordId && c.Status == SubscriptionClaimStatus.Pending);

            if (alreadyPending)
            {
                TempData["Message"] = "You already have a claim awaiting confirmation.";
                return RedirectToAction(nameof(Index));
            }

            var settings = await _context.AppSettings.FirstOrDefaultAsync();

            var claim = new SubscriptionClaim
            {
                LandlordId = landlordId,
                Amount = settings?.MonthlySubscriptionFee ?? 0m
            };

            _context.SubscriptionClaims.Add(claim);
            await _context.SaveChangesAsync();

            if (proof != null)
            {
                if (_proofStorage.IsAllowed(proof, out var extension))
                {
                    claim.ProofFileName = await _proofStorage.SaveAsync("subscription", claim.Id, proof, extension);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    TempData["Message"] = "Claim submitted, but the receipt was skipped — it must be a JPG, PNG, or PDF up to 5 MB.";
                }
            }

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientId = admin.Id,
                    Type = NotificationType.SubscriptionClaimed,
                    Message = $"A landlord marked their {claim.Amount:C} subscription payment as paid.",
                    LinkController = "Subscription",
                    LinkAction = "Manage",
                    RelatedEntityId = claim.Id
                });
            }
            await _context.SaveChangesAsync();

            TempData["Message"] ??= "Thanks — we'll confirm your payment shortly.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var claims = await _context.SubscriptionClaims
                .Include(c => c.Landlord)
                .OrderByDescending(c => c.ClaimedAt)
                .ToListAsync();

            return View(claims);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var claim = await _context.SubscriptionClaims
                .Include(c => c.Landlord)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
            {
                return NotFound();
            }

            if (claim.Status == SubscriptionClaimStatus.Pending)
            {
                claim.Status = SubscriptionClaimStatus.Confirmed;
                claim.ConfirmedAt = DateTime.UtcNow;

                var landlord = claim.Landlord!;
                var extendFrom = landlord.SubscriptionActiveUntil.HasValue && landlord.SubscriptionActiveUntil.Value > DateTime.UtcNow
                    ? landlord.SubscriptionActiveUntil.Value
                    : DateTime.UtcNow;
                landlord.SubscriptionActiveUntil = extendFrom.AddDays(30);

                _context.Notifications.Add(new Notification
                {
                    RecipientId = landlord.Id,
                    Type = NotificationType.SubscriptionConfirmed,
                    Message = $"Your {claim.Amount:C} subscription payment was confirmed. You're active until {landlord.SubscriptionActiveUntil.Value:MMM d, yyyy}.",
                    LinkController = "Subscription",
                    LinkAction = "Index",
                    RelatedEntityId = claim.Id
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? note)
        {
            var claim = await _context.SubscriptionClaims
                .Include(c => c.Landlord)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
            {
                return NotFound();
            }

            if (claim.Status == SubscriptionClaimStatus.Pending)
            {
                claim.Status = SubscriptionClaimStatus.Rejected;
                claim.AdminNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

                _context.Notifications.Add(new Notification
                {
                    RecipientId = claim.LandlordId,
                    Type = NotificationType.SubscriptionRejected,
                    Message = $"Your {claim.Amount:C} subscription claim wasn't confirmed{(claim.AdminNote != null ? $": {claim.AdminNote}" : ".")}",
                    LinkController = "Subscription",
                    LinkAction = "Index",
                    RelatedEntityId = claim.Id
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Manage));
        }
    }
}
