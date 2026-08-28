using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Extensions;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    [Authorize]
    public class UtilityBillController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UtilityBillController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Landlord")]
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
        [Authorize(Roles = "Landlord")]
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

        [HttpPost]
        [Authorize(Roles = "Tenant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RaiseDispute(int shareId, string reason)
        {
            var tenantId = _userManager.GetUserId(User)!;

            var share = await _context.UtilityBillShares
                .Include(s => s.UtilityBill)
                    .ThenInclude(b => b!.Property)
                .FirstOrDefaultAsync(s => s.Id == shareId && s.TenantId == tenantId);

            if (share == null)
            {
                return NotFound();
            }

            if (share.DisputeStatus == DisputeStatus.None)
            {
                share.DisputeStatus = DisputeStatus.Open;
                share.DisputeReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
                share.DisputeRaisedAt = DateTime.UtcNow;

                var landlordId = share.UtilityBill!.Property!.LandlordId;
                _context.Notifications.Add(new Notification
                {
                    RecipientId = landlordId,
                    Type = NotificationType.DisputeRaised,
                    Message = $"A tenant disputed a {share.UtilityBill.BillType.Humanize()} charge of {share.ShareAmount:C}.",
                    LinkController = "UtilityBill",
                    LinkAction = "Index",
                    RelatedEntityId = share.Id
                });

                await _context.SaveChangesAsync();
                TempData["Message"] = "Dispute submitted. The landlord has been notified.";
            }

            return RedirectToAction("Index", "Tenant");
        }

        [HttpPost]
        [Authorize(Roles = "Landlord")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveDispute(int shareId, string resolution, decimal? adjustedAmount)
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

            if (share.DisputeStatus == DisputeStatus.Open)
            {
                share.DisputeStatus = DisputeStatus.Resolved;
                share.DisputeResolution = string.IsNullOrWhiteSpace(resolution) ? null : resolution.Trim();
                share.DisputeResolvedAt = DateTime.UtcNow;

                if (adjustedAmount is > 0)
                {
                    share.ShareAmount = adjustedAmount.Value;
                }

                _context.Notifications.Add(new Notification
                {
                    RecipientId = share.TenantId,
                    Type = NotificationType.DisputeResolved,
                    Message = $"Your dispute was resolved: {share.DisputeResolution ?? "no additional notes"}.",
                    LinkController = "Tenant",
                    LinkAction = "Index",
                    RelatedEntityId = share.Id
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Landlord")]
        public async Task<IActionResult> Create(int propertyId)
        {
            var property = await GetOwnedPropertyAsync(propertyId);
            if (property == null)
            {
                return NotFound();
            }

            var tenants = await GetAllActiveTenantsAsync(propertyId);
            if (!tenants.Any())
            {
                TempData["Message"] = "This property has no active tenants to split a bill across yet.";
                return RedirectToAction(nameof(Index));
            }

            var settings = await _context.AppSettings.FirstOrDefaultAsync();

            var model = new UtilityBillFormViewModel
            {
                PropertyId = property.Id,
                PropertyName = property.Name,
                SplitMethod = settings?.DefaultUtilitySplitMethod ?? UtilityBillSplitMethod.Equal,
                Tenants = await BuildTenantInputsAsync(propertyId, tenants)
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Landlord")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UtilityBillFormViewModel model)
        {
            var property = await GetOwnedPropertyAsync(model.PropertyId);
            if (property == null)
            {
                return NotFound();
            }

            var activeTenants = await GetActiveTenantsAsync(model.PropertyId, model.BillType);
            var activeTenantIds = activeTenants.Select(t => t.Id).ToHashSet();

            if (model.SplitMethod == UtilityBillSplitMethod.CustomPercentage)
            {
                var total = model.Tenants.Where(t => activeTenantIds.Contains(t.TenantId)).Sum(t => t.Percentage);
                if (Math.Abs(total - 100m) > 0.01m)
                {
                    ModelState.AddModelError(string.Empty, $"Custom percentages must add up to 100% (currently {total}%).");
                }
            }
            else if (model.SplitMethod == UtilityBillSplitMethod.PerUnitConsumption)
            {
                if (model.TotalUnitsConsumed is null || model.TotalUnitsConsumed <= 0)
                {
                    ModelState.AddModelError(nameof(model.TotalUnitsConsumed), "Enter the total units consumed for this bill.");
                }
            }

            if (!ModelState.IsValid)
            {
                var allTenants = await GetAllActiveTenantsAsync(model.PropertyId);
                model.PropertyName = property.Name;
                model.Tenants = await BuildTenantInputsAsync(model.PropertyId, allTenants);
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
                SplitMethod = model.SplitMethod,
                TotalUnitsConsumed = model.SplitMethod == UtilityBillSplitMethod.PerUnitConsumption ? model.TotalUnitsConsumed : null
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
            else if (model.SplitMethod == UtilityBillSplitMethod.PerUnitConsumption)
            {
                var rate = model.Amount / model.TotalUnitsConsumed!.Value;

                foreach (var input in model.Tenants.Where(t => activeTenantIds.Contains(t.TenantId)))
                {
                    var units = input.UnitsConsumed ?? 0m;
                    shares.Add(new UtilityBillShare
                    {
                        TenantId = input.TenantId,
                        UnitsConsumed = units,
                        ShareAmount = Math.Round(rate * units, 2)
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

        private async Task<List<ApplicationUser>> GetActiveTenantsAsync(int propertyId, UtilityBillType billType)
        {
            var today = DateTime.UtcNow.Date;
            var query = _context.Leases
                .Where(l => (l.EndDate == null || l.EndDate >= today) && l.Unit!.PropertyId == propertyId);

            query = billType switch
            {
                UtilityBillType.Electricity => query.Where(l => !l.Unit!.HasIndividualElectricityMeter),
                UtilityBillType.Water => query.Where(l => !l.Unit!.HasIndividualWaterMeter),
                _ => query
            };

            return await query
                .Select(l => l.Tenant!)
                .Distinct()
                .ToListAsync();
        }

        // Unfiltered by meter flags — used to render the form so switching Bill Type client-side
        // can reveal/hide tenants without a round-trip. Real exclusion is enforced in POST via GetActiveTenantsAsync.
        private async Task<List<ApplicationUser>> GetAllActiveTenantsAsync(int propertyId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Leases
                .Where(l => (l.EndDate == null || l.EndDate >= today) && l.Unit!.PropertyId == propertyId)
                .Select(l => l.Tenant!)
                .Distinct()
                .ToListAsync();
        }

        private async Task<List<TenantShareInputViewModel>> BuildTenantInputsAsync(int propertyId, List<ApplicationUser> tenants)
        {
            var today = DateTime.UtcNow.Date;
            var meterFlags = await _context.Leases
                .Where(l => (l.EndDate == null || l.EndDate >= today) && l.Unit!.PropertyId == propertyId)
                .Select(l => new { l.TenantId, l.Unit!.HasIndividualElectricityMeter, l.Unit.HasIndividualWaterMeter })
                .ToListAsync();

            return tenants.Select(t => new TenantShareInputViewModel
            {
                TenantId = t.Id,
                TenantName = t.FullName,
                ExcludedForElectricity = meterFlags.Any(f => f.TenantId == t.Id && f.HasIndividualElectricityMeter),
                ExcludedForWater = meterFlags.Any(f => f.TenantId == t.Id && f.HasIndividualWaterMeter)
            }).ToList();
        }
    }
}
