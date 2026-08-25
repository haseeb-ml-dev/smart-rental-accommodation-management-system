using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    [Authorize(Roles = "Tenant")]
    public class TenantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TenantController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var tenantId = _userManager.GetUserId(User)!;

            var lease = await _context.Leases
                .Include(l => l.Unit)
                    .ThenInclude(u => u!.Property)
                .Where(l => l.TenantId == tenantId && l.EndDate == null)
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

            var invoices = await _context.RentInvoices
                .Where(i => i.LeaseId == lease.Id)
                .OrderByDescending(i => i.DueDate)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;

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
    }
}
