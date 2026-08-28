using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.ViewModels;
using System.Diagnostics;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity is { IsAuthenticated: true })
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                if (User.IsInRole("Landlord"))
                {
                    return RedirectToAction("Index", "Landlord");
                }
                if (User.IsInRole("Tenant"))
                {
                    return RedirectToAction("Index", "Tenant");
                }
            }

            var today = DateTime.UtcNow.Date;

            var units = await _context.Units
                .Include(u => u.Property)
                .Include(u => u.Leases)
                .Include(u => u.Bookings)
                .Include(u => u.Images)
                .Where(u => u.Property!.IsActive)
                .ToListAsync();

            var vm = units.Select(u => new PublicListingViewModel
            {
                UnitId = u.Id,
                CoverImageFileName = (u.Images.FirstOrDefault(i => i.IsCover) ?? u.Images.FirstOrDefault())?.FileName,
                PropertyName = u.Property?.Name ?? string.Empty,
                Address = u.Property?.Address ?? string.Empty,
                City = u.Property?.City,
                Latitude = u.Property?.Latitude,
                Longitude = u.Property?.Longitude,
                UnitName = u.Name,
                UnitType = u.UnitType,
                BhkType = u.BhkType,
                MonthlyRent = u.MonthlyRent,
                Capacity = u.Capacity,
                BookableSlots = u.BookableSlots,
                ActiveLeaseCount = u.Leases.Count(l => l.EndDate == null || l.EndDate >= today)
            })
            .OrderBy(u => u.PropertyName).ThenBy(u => u.UnitName)
            .ToList();

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
