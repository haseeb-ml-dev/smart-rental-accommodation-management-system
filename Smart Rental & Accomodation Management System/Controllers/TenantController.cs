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
    [Authorize(Roles = "Tenant")]
    public class TenantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PaymentSlipStorage _slipStorage;

        public TenantController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, PaymentSlipStorage slipStorage)
        {
            _context = context;
            _userManager = userManager;
            _slipStorage = slipStorage;
        }

        public async Task<IActionResult> Index()
        {
            var tenantId = _userManager.GetUserId(User)!;
            var today = DateTime.UtcNow.Date;

            var lease = await _context.Leases
                .Include(l => l.Unit)
                    .ThenInclude(u => u!.Property)
                .Include(l => l.Occupants)
                .Where(l => l.TenantId == tenantId && (l.EndDate == null || l.EndDate >= today))
                .OrderByDescending(l => l.StartDate)
                .FirstOrDefaultAsync();

            var vm = new TenantDashboardViewModel();

            if (lease == null)
            {
                return View(vm);
            }

            vm.HasActiveLease = true;
            vm.PropertyName = lease.Unit?.Property?.Name;
            vm.UnitName = lease.Unit?.Name;
            vm.MonthlyRent = lease.Unit?.MonthlyRent ?? 0;
            vm.LeaseId = lease.Id;
            vm.CanManageOccupants = lease.Unit?.UnitType == UnitType.FamilyUnit;
            vm.Occupants = lease.Occupants;

            var invoices = await _context.RentInvoices
                .Where(i => i.LeaseId == lease.Id)
                .OrderByDescending(i => i.DueDate)
                .ToListAsync();

            vm.TotalPaid = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount);
            vm.OutstandingBalance = invoices.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => i.Amount);
            vm.OverdueInvoiceCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue);
            vm.NextDueInvoice = invoices
                .Where(i => i.Status != InvoiceStatus.Paid)
                .OrderBy(i => i.DueDate)
                .FirstOrDefault();
            vm.Invoices = invoices.Take(10).ToList();

            for (int monthsAgo = 5; monthsAgo >= 0; monthsAgo--)
            {
                var monthDate = new DateTime(today.Year, today.Month, 1).AddMonths(-monthsAgo);
                var monthInvoices = invoices.Where(i => i.DueDate.Year == monthDate.Year && i.DueDate.Month == monthDate.Month).ToList();

                vm.MonthlyPaymentHistory.Add(new MonthlyCollectionPoint
                {
                    Label = monthDate.ToString("MMM yyyy"),
                    Collected = monthInvoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount),
                    Outstanding = monthInvoices.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => i.Amount)
                });
            }

            vm.UtilityShares = await _context.UtilityBillShares
                .Include(s => s.UtilityBill)
                    .ThenInclude(b => b!.Property)
                .Where(s => s.TenantId == tenantId)
                .OrderByDescending(s => s.UtilityBill!.DueDate)
                .Take(10)
                .ToListAsync();
            vm.UtilityOutstandingBalance = vm.UtilityShares.Where(s => !s.IsPaid).Sum(s => s.ShareAmount);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(PaymentSlipStorage.MaxFileSizeBytes)]
        public async Task<IActionResult> MarkInvoicePaidByTenant(int invoiceId, IFormFile? slip)
        {
            var tenantId = _userManager.GetUserId(User)!;
            var tenantName = (await _userManager.GetUserAsync(User))?.FullName;

            var invoice = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Unit)
                        .ThenInclude(u => u!.Property)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Lease!.TenantId == tenantId);

            if (invoice == null)
            {
                return NotFound();
            }

            if (invoice.Status == InvoiceStatus.Paid)
            {
                return RedirectToAction(nameof(Index));
            }

            if (slip != null)
            {
                if (!_slipStorage.IsAllowed(slip, out var extension))
                {
                    TempData["Message"] = "Payment slip must be a JPG, PNG, or PDF up to 5 MB.";
                    return RedirectToAction(nameof(Index));
                }

                invoice.PaymentSlipFileName = await _slipStorage.SaveAsync("rent", invoice.Id, slip, extension);
            }

            invoice.TenantMarkedPaidAt = DateTime.UtcNow;

            var landlordId = invoice.Lease?.Unit?.Property?.LandlordId;
            if (landlordId != null)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientId = landlordId,
                    Type = NotificationType.RentPaymentClaimed,
                    Message = $"{tenantName ?? "A tenant"} marked their {invoice.PeriodMonth}/{invoice.PeriodYear} rent ({invoice.Amount:C}) as paid.",
                    LinkController = "Landlord",
                    LinkAction = "Index",
                    RelatedEntityId = invoice.Id
                });
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Landlord notified. They'll confirm once they've verified the payment.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(PaymentSlipStorage.MaxFileSizeBytes)]
        public async Task<IActionResult> MarkUtilitySharePaidByTenant(int shareId, IFormFile? slip)
        {
            var tenantId = _userManager.GetUserId(User)!;

            var share = await _context.UtilityBillShares
                .Include(s => s.UtilityBill)
                    .ThenInclude(b => b!.Property)
                .Include(s => s.Tenant)
                .FirstOrDefaultAsync(s => s.Id == shareId && s.TenantId == tenantId);

            if (share == null)
            {
                return NotFound();
            }

            if (share.IsPaid)
            {
                return RedirectToAction(nameof(Index));
            }

            if (slip != null)
            {
                if (!_slipStorage.IsAllowed(slip, out var extension))
                {
                    TempData["Message"] = "Payment slip must be a JPG, PNG, or PDF up to 5 MB.";
                    return RedirectToAction(nameof(Index));
                }

                share.PaymentSlipFileName = await _slipStorage.SaveAsync("utility", share.Id, slip, extension);
            }

            share.TenantMarkedPaidAt = DateTime.UtcNow;

            var landlordId = share.UtilityBill?.Property?.LandlordId;
            if (landlordId != null)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientId = landlordId,
                    Type = NotificationType.UtilityPaymentClaimed,
                    Message = $"{share.Tenant?.FullName ?? "A tenant"} marked their {share.UtilityBill!.BillType.Humanize()} share ({share.ShareAmount:C}) as paid.",
                    LinkController = "UtilityBill",
                    LinkAction = "Index",
                    RelatedEntityId = share.Id
                });
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Landlord notified. They'll confirm once they've verified the payment.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOccupant(int leaseId, string name, string? phone)
        {
            var lease = await GetOwnedFamilyUnitLeaseAsync(leaseId);
            if (lease == null)
            {
                return NotFound();
            }

            var trimmedName = name?.Trim();
            if (!string.IsNullOrEmpty(trimmedName))
            {
                _context.LeaseOccupants.Add(new LeaseOccupant
                {
                    LeaseId = lease.Id,
                    Name = trimmedName,
                    Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveOccupant(int occupantId)
        {
            var tenantId = _userManager.GetUserId(User)!;

            var occupant = await _context.LeaseOccupants
                .Include(o => o.Lease)
                .FirstOrDefaultAsync(o => o.Id == occupantId && o.Lease!.TenantId == tenantId);

            if (occupant == null)
            {
                return NotFound();
            }

            _context.LeaseOccupants.Remove(occupant);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<Lease?> GetOwnedFamilyUnitLeaseAsync(int leaseId)
        {
            var tenantId = _userManager.GetUserId(User)!;
            var today = DateTime.UtcNow.Date;
            return await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.Id == leaseId && l.TenantId == tenantId && (l.EndDate == null || l.EndDate >= today) && l.Unit!.UnitType == UnitType.FamilyUnit);
        }
    }
}
