using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    [Authorize(Roles = "Landlord")]
    public class UtilityBillController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UtilityBillController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var landlordId = _userManager.GetUserId(User)!;

            var bills = await _context.UtilityBills
                .Include(b => b.Property)
                .Include(b => b.Shares)
                    .ThenInclude(s => s.Tenant)
                .Where(b => b.Property!.LandlordId == landlordId)
                .OrderByDescending(b => b.DueDate)
                .ToListAsync();

            return View(bills);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkSharePaid(int shareId)
        {
            var landlordId = _userManager.GetUserId(User)!;

            var share = await _context.UtilityBillShares
                .Include(s => s.UtilityBill)
                    .ThenInclude(b => b!.Property)
                .FirstOrDefaultAsync(s => s.Id == shareId && s.UtilityBill!.Property!.LandlordId == landlordId);

            if (share == null)
            {
                return NotFound();
            }

            if (!share.IsPaid)
            {
                share.IsPaid = true;
                share.PaidDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Create(int propertyId)
        {
            var property = await GetOwnedPropertyAsync(propertyId);
            if (property == null)
            {
                return NotFound();
            }

            var tenants = await GetActiveTenantsAsync(propertyId);
            if (!tenants.Any())
            {
                TempData["Message"] = "This property has no active tenants to split a bill across yet.";
                return RedirectToAction(nameof(Index));
            }

            var model = new UtilityBillFormViewModel
            {
                PropertyId = property.Id,
                PropertyName = property.Name,
                Tenants = tenants.Select(t => new TenantShareInputViewModel { TenantId = t.Id, TenantName = t.FullName }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UtilityBillFormViewModel model)
        {
            var property = await GetOwnedPropertyAsync(model.PropertyId);
            if (property == null)
            {
                return NotFound();
            }

            var activeTenants = await GetActiveTenantsAsync(model.PropertyId);
            var activeTenantIds = activeTenants.Select(t => t.Id).ToHashSet();

            if (model.SplitMethod == UtilityBillSplitMethod.CustomPercentage)
            {
                var total = model.Tenants.Where(t => activeTenantIds.Contains(t.TenantId)).Sum(t => t.Percentage);
                if (Math.Abs(total - 100m) > 0.01m)
                {
                    ModelState.AddModelError(string.Empty, $"Custom percentages must add up to 100% (currently {total}%).");
                }
            }

            if (!ModelState.IsValid)
            {
                model.PropertyName = property.Name;
                model.Tenants = activeTenants.Select(t => new TenantShareInputViewModel { TenantId = t.Id, TenantName = t.FullName }).ToList();
                return View(model);
            }

            var bill = new UtilityBill
            {
                PropertyId = property.Id,
                BillType = model.BillType,
                Amount = model.Amount,
                PeriodMonth = model.PeriodMonth,
                PeriodYear = model.PeriodYear,
                DueDate = model.DueDate,
                SplitMethod = model.SplitMethod
            };

            var shares = new List<UtilityBillShare>();

            if (model.SplitMethod == UtilityBillSplitMethod.Equal)
            {
                var count = activeTenants.Count;
                var baseShare = Math.Floor(model.Amount / count * 100) / 100;
                var remainder = model.Amount - baseShare * count;

                for (int i = 0; i < count; i++)
                {
                    shares.Add(new UtilityBillShare
                    {
                        TenantId = activeTenants[i].Id,
                        ShareAmount = i == count - 1 ? baseShare + remainder : baseShare
                    });
                }
            }
            else
            {
                foreach (var input in model.Tenants.Where(t => activeTenantIds.Contains(t.TenantId)))
                {
                    shares.Add(new UtilityBillShare
                    {
                        TenantId = input.TenantId,
                        Percentage = input.Percentage,
                        ShareAmount = Math.Round(model.Amount * input.Percentage / 100m, 2)
                    });
                }
            }

            bill.Shares = shares;
            _context.UtilityBills.Add(bill);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<Property?> GetOwnedPropertyAsync(int propertyId)
        {
            var landlordId = _userManager.GetUserId(User)!;
            return await _context.Properties.FirstOrDefaultAsync(p => p.Id == propertyId && p.LandlordId == landlordId);
        }

        private async Task<List<ApplicationUser>> GetActiveTenantsAsync(int propertyId)
        {
            return await _context.Leases
                .Where(l => l.EndDate == null && l.Unit!.PropertyId == propertyId)
                .Select(l => l.Tenant!)
                .Distinct()
                .ToListAsync();
        }
    }
}
