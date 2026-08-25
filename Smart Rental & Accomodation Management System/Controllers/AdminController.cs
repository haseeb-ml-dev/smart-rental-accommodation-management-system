using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalLandlords = (await _userManager.GetUsersInRoleAsync("Landlord")).Count,
                TotalTenants = (await _userManager.GetUsersInRoleAsync("Tenant")).Count
            };

            var properties = await _context.Properties
                .Include(p => p.Landlord)
                .Include(p => p.Units)
                .ToListAsync();

            vm.TotalProperties = properties.Count;
            vm.TotalUnits = properties.Sum(p => p.Units.Count);
            vm.Properties = properties;

            var invoices = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Unit)
                        .ThenInclude(u => u!.Property)
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Tenant)
                .OrderByDescending(i => i.DueDate)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;

            vm.CollectedThisMonth = invoices.Where(i => i.DueDate.Year == today.Year && i.DueDate.Month == today.Month && i.Status == InvoiceStatus.Paid).Sum(i => i.Amount);
            vm.OutstandingThisMonth = invoices.Where(i => i.DueDate.Year == today.Year && i.DueDate.Month == today.Month && i.Status != InvoiceStatus.Paid).Sum(i => i.Amount);
            vm.OverdueInvoiceCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue);
            vm.RecentInvoices = invoices.Take(10).ToList();

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
