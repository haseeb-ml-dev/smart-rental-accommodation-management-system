using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Extensions;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.Services;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    [Authorize]
    public class MaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PaymentSlipStorage _photoStorage;

        public MaintenanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, PaymentSlipStorage photoStorage)
        {
            _context = context;
            _userManager = userManager;
            _photoStorage = photoStorage;
        }

        [Authorize(Roles = "Tenant")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var lease = await GetCurrentLeaseAsync();
            if (lease == null)
            {
                TempData["Message"] = "You don't have an active lease yet, so there's no unit to report an issue on.";
                return RedirectToAction("Index", "Tenant");
            }

            return View(new MaintenanceRequestFormViewModel());
        }

        [Authorize(Roles = "Tenant")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(PaymentSlipStorage.MaxFileSizeBytes)]
        public async Task<IActionResult> Create(MaintenanceRequestFormViewModel model)
        {
            var lease = await GetCurrentLeaseAsync();
            if (lease == null)
            {
                TempData["Message"] = "You don't have an active lease yet, so there's no unit to report an issue on.";
                return RedirectToAction("Index", "Tenant");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var tenantId = _userManager.GetUserId(User)!;
            var tenantName = (await _userManager.GetUserAsync(User))?.FullName;

            var request = new MaintenanceRequest
            {
                UnitId = lease.UnitId,
                TenantId = tenantId,
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                Category = model.Category,
                Priority = model.Priority
            };

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            if (model.Photo != null)
            {
                if (_photoStorage.IsAllowed(model.Photo, out var extension))
                {
                    request.PhotoFileName = await _photoStorage.SaveAsync("maintenance", request.Id, model.Photo, extension);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    TempData["Message"] = "Request submitted, but the photo was skipped — it must be a JPG, PNG, or PDF up to 5 MB.";
                }
            }

            var landlordId = lease.Unit!.Property!.LandlordId;
            _context.Notifications.Add(new Notification
            {
                RecipientId = landlordId,
                Type = NotificationType.MaintenanceRequestCreated,
                Message = $"{tenantName ?? "A tenant"} reported an issue on {lease.Unit.Name}: {request.Title}",
                LinkController = "Maintenance",
                LinkAction = "Index",
                RelatedEntityId = request.Id
            });
            await _context.SaveChangesAsync();

            TempData["Message"] ??= "Maintenance request submitted. Your landlord has been notified.";
            return RedirectToAction(nameof(MyRequests));
        }

        [Authorize(Roles = "Tenant")]
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            var tenantId = _userManager.GetUserId(User)!;

            var requests = await _context.MaintenanceRequests
                .Include(m => m.Unit)
                    .ThenInclude(u => u!.Property)
                .Where(m => m.TenantId == tenantId)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new MaintenanceRequestListItemViewModel
                {
                    Id = m.Id,
                    PropertyName = m.Unit!.Property!.Name,
                    UnitName = m.Unit.Name,
                    Title = m.Title,
                    Description = m.Description,
                    Category = m.Category,
                    Priority = m.Priority,
                    Status = m.Status,
                    PhotoFileName = m.PhotoFileName,
                    LandlordNote = m.LandlordNote,
                    CreatedAt = m.CreatedAt,
                    ResolvedAt = m.ResolvedAt
                })
                .ToListAsync();

            return View(requests);
        }

        [Authorize(Roles = "Landlord")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var landlordId = _userManager.GetUserId(User)!;

            var requests = await _context.MaintenanceRequests
                .Include(m => m.Unit)
                    .ThenInclude(u => u!.Property)
                .Include(m => m.Tenant)
                .Where(m => m.Unit!.Property!.LandlordId == landlordId)
                .OrderBy(m => m.Status)
                    .ThenByDescending(m => m.Priority)
                    .ThenByDescending(m => m.CreatedAt)
                .Select(m => new MaintenanceRequestListItemViewModel
                {
                    Id = m.Id,
                    PropertyName = m.Unit!.Property!.Name,
                    UnitName = m.Unit.Name,
                    TenantName = m.Tenant!.FullName,
                    Title = m.Title,
                    Description = m.Description,
                    Category = m.Category,
                    Priority = m.Priority,
                    Status = m.Status,
                    PhotoFileName = m.PhotoFileName,
                    LandlordNote = m.LandlordNote,
                    CreatedAt = m.CreatedAt,
                    ResolvedAt = m.ResolvedAt
                })
                .ToListAsync();

            return View(requests);
        }

        [Authorize(Roles = "Landlord")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int requestId, MaintenanceStatus status, string? landlordNote)
        {
            var landlordId = _userManager.GetUserId(User)!;

            var request = await _context.MaintenanceRequests
                .Include(m => m.Unit)
                .FirstOrDefaultAsync(m => m.Id == requestId && m.Unit!.Property!.LandlordId == landlordId);

            if (request == null)
            {
                return NotFound();
            }

            request.Status = status;
            request.LandlordNote = string.IsNullOrWhiteSpace(landlordNote) ? request.LandlordNote : landlordNote.Trim();
            request.ResolvedAt = status == MaintenanceStatus.Resolved ? DateTime.UtcNow : null;

            _context.Notifications.Add(new Notification
            {
                RecipientId = request.TenantId,
                Type = NotificationType.MaintenanceStatusUpdated,
                Message = $"Your maintenance request \"{request.Title}\" is now {status.Humanize()}.",
                LinkController = "Maintenance",
                LinkAction = "MyRequests",
                RelatedEntityId = request.Id
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<Lease?> GetCurrentLeaseAsync()
        {
            var tenantId = _userManager.GetUserId(User)!;
            var today = DateTime.UtcNow.Date;

            return await _context.Leases
                .Include(l => l.Unit)
                    .ThenInclude(u => u!.Property)
                .Where(l => l.TenantId == tenantId && (l.EndDate == null || l.EndDate >= today))
                .OrderByDescending(l => l.StartDate)
                .FirstOrDefaultAsync();
        }
    }
}
