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
    public class LandlordController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LandlordController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var landlordId = _userManager.GetUserId(User)!;

            var units = await _context.Units
                .Include(u => u.Property)
                .Include(u => u.Leases)
                .Where(u => u.Property!.LandlordId == landlordId)
                .ToListAsync();

            var unitIds = units.Select(u => u.Id).ToHashSet();

            var activeLeaseUnitIds = units
                .SelectMany(u => u.Leases)
                .Where(l => l.EndDate == null)
                .Select(l => l.UnitId)
                .ToHashSet();

            var invoices = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Unit)
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Tenant)
                .Where(i => unitIds.Contains(i.Lease!.UnitId))
                .OrderByDescending(i => i.DueDate)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;

            var vm = new LandlordDashboardViewModel
            {
                TotalProperties = units.Select(u => u.PropertyId).Distinct().Count(),
                TotalUnits = units.Count,
                OccupiedUnits = activeLeaseUnitIds.Count,
                VacantUnits = units.Count - activeLeaseUnitIds.Count,
                ActiveTenants = units.SelectMany(u => u.Leases).Where(l => l.EndDate == null).Select(l => l.TenantId).Distinct().Count(),
                CollectedThisMonth = invoices.Where(i => i.DueDate.Year == today.Year && i.DueDate.Month == today.Month && i.Status == InvoiceStatus.Paid).Sum(i => i.Amount),
                OutstandingThisMonth = invoices.Where(i => i.DueDate.Year == today.Year && i.DueDate.Month == today.Month && i.Status != InvoiceStatus.Paid).Sum(i => i.Amount),
                OverdueInvoiceCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue),
                RecentInvoices = invoices.Take(10).ToList()
            };

            for (int monthsAgo = 5; monthsAgo >= 0; monthsAgo--)
            {
                var monthDate = new DateTime(today.Year, today.Month, 1).AddMonths(-monthsAgo);
                var monthInvoices = invoices.Where(i => i.DueDate.Year == monthDate.Year && i.DueDate.Month == monthDate.Month).ToList();

                vm.MonthlyCollection.Add(new MonthlyCollectionPoint
                {
                    Label = monthDate.ToString("MMM yyyy"),
                    Collected = monthInvoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount),
                    Outstanding = monthInvoices.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => i.Amount)
                });
            }

            vm.OutstandingUtilities = await _context.UtilityBillShares
                .Where(s => !s.IsPaid && s.UtilityBill!.Property!.LandlordId == landlordId)
                .SumAsync(s => s.ShareAmount);

            return View(vm);
        }

        public async Task<IActionResult> OverdueTenants()
        {
            var landlordId = _userManager.GetUserId(User)!;

            var overdueInvoices = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Unit)
                        .ThenInclude(u => u!.Property)
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Tenant)
                .Where(i => i.Status == InvoiceStatus.Overdue && i.Lease!.Unit!.Property!.LandlordId == landlordId)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var overdueShares = await _context.UtilityBillShares
                .Include(s => s.UtilityBill)
                    .ThenInclude(b => b!.Property)
                .Include(s => s.Tenant)
                .Where(s => !s.IsPaid && s.UtilityBill!.DueDate < today && s.UtilityBill.Property!.LandlordId == landlordId)
                .ToListAsync();

            var groups = new List<OverdueTenantGroupViewModel>();

            foreach (var invoiceGroup in overdueInvoices.GroupBy(i => i.Lease!.TenantId))
            {
                var first = invoiceGroup.First();
                groups.Add(new OverdueTenantGroupViewModel
                {
                    TenantName = first.Lease!.Tenant!.FullName,
                    PropertyName = first.Lease.Unit!.Property!.Name,
                    UnitName = first.Lease.Unit.Name,
                    OverdueInvoices = invoiceGroup.OrderBy(i => i.DueDate).ToList(),
                    OverdueUtilityShares = overdueShares.Where(s => s.TenantId == invoiceGroup.Key).ToList()
                });
            }

            var invoiceTenantIds = overdueInvoices.Select(i => i.Lease!.TenantId).ToHashSet();
            foreach (var shareGroup in overdueShares.Where(s => !invoiceTenantIds.Contains(s.TenantId)).GroupBy(s => s.TenantId))
            {
                var first = shareGroup.First();
                groups.Add(new OverdueTenantGroupViewModel
                {
                    TenantName = first.Tenant!.FullName,
                    PropertyName = first.UtilityBill!.Property!.Name,
                    UnitName = string.Empty,
                    OverdueUtilityShares = shareGroup.ToList()
                });
            }

            return View(groups.OrderByDescending(g => g.TotalOverdue).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkInvoicePaid(int invoiceId)
        {
            var landlordId = _userManager.GetUserId(User)!;

            var invoice = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Unit)
                        .ThenInclude(u => u!.Property)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Lease!.Unit!.Property!.LandlordId == landlordId);

            if (invoice == null)
            {
                return NotFound();
            }

            if (invoice.Status != InvoiceStatus.Paid)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
