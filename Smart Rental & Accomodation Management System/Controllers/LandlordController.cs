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

            return View(vm);
        }
    }
}
