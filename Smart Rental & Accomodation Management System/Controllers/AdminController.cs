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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePropertyActive(int propertyId)
        {
            var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == propertyId);
            if (property == null)
            {
                return NotFound();
            }

            property.IsActive = !property.IsActive;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var vm = new List<AdminUserViewModel>();

            foreach (var user in users)
            {
                vm.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                    IsLockedOut = await _userManager.IsLockedOutAsync(user)
                });
            }

            return View(vm.OrderBy(u => u.FullName).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserLockout(string userId)
        {
            var currentAdminId = _userManager.GetUserId(User)!;
            if (userId == currentAdminId)
            {
                TempData["Message"] = "You can't suspend your own account.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Message"] = "Admin accounts can't be suspended.";
                return RedirectToAction(nameof(Users));
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["Message"] = $"{user.FullName} has been reactivated.";
            }
            else
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["Message"] = $"{user.FullName} has been suspended.";
            }

            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Unit)
                    .ThenInclude(u => u!.Property)
                .Include(b => b.Tenant)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BookingListItemViewModel
                {
                    BookingId = b.Id,
                    PropertyName = b.Unit!.Property!.Name,
                    UnitName = b.Unit.Name,
                    TenantName = b.Tenant!.FullName,
                    StartDate = b.StartDate,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(int bookingId)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == BookingStatus.Pending)
            {
                booking.Status = BookingStatus.Rejected;
                booking.DecisionDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Bookings));
        }
    }
}
