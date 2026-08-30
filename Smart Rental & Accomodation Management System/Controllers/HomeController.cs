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
        private const int PageSize = 9;

        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(UnitSearchFilter filter)
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

            filter ??= new UnitSearchFilter();
            if (filter.Page < 1)
            {
                filter.Page = 1;
            }

            var today = DateTime.UtcNow.Date;

            var query = _context.Units
                .Include(u => u.Property)
                .Include(u => u.Leases)
                .Include(u => u.Bookings)
                .Include(u => u.Images)
                .Where(u => u.Property!.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                query = query.Where(u => u.Property!.City == filter.City);
            }
            if (filter.MinRent.HasValue)
            {
                query = query.Where(u => u.MonthlyRent >= filter.MinRent.Value);
            }
            if (filter.MaxRent.HasValue)
            {
                query = query.Where(u => u.MonthlyRent <= filter.MaxRent.Value);
            }
            if (filter.UnitType.HasValue)
            {
                query = query.Where(u => u.UnitType == filter.UnitType.Value);
            }

            var totalCount = await query.CountAsync();

            var units = await query
                .OrderBy(u => u.Property!.Name).ThenBy(u => u.Name)
                .Skip((filter.Page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var cities = await _context.Properties
                .Where(p => p.IsActive && p.City != null)
                .Select(p => p.City!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var vm = new PublicBrowseViewModel
            {
                Filter = filter,
                Cities = cities,
                TotalCount = totalCount,
                PageSize = PageSize,
                Units = units.Select(u => new PublicListingViewModel
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
                }).ToList()
            };

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
