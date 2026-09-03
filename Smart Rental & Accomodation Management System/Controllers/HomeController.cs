using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.Services;
using Smart_Rental___Accomodation_Management_System.ViewModels;
using System.Diagnostics;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    public class HomeController : Controller
    {
        private const int PageSize = 9;

        private readonly ApplicationDbContext _context;
        private readonly LocationOptionsService _locationOptions;

        public HomeController(ApplicationDbContext context, LocationOptionsService locationOptions)
        {
            _context = context;
            _locationOptions = locationOptions;
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

            var vm = new HomeStatsViewModel
            {
                TotalUnits = await _context.Units.CountAsync(u => u.Property!.IsActive),
                TotalCities = await _context.Units
                    .Where(u => u.Property!.IsActive && u.Property!.City != null)
                    .Select(u => u.Property!.City)
                    .Distinct()
                    .CountAsync()
            };

            return View(vm);
        }

        public async Task<IActionResult> Listings(UnitSearchFilter filter)
        {
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
            if (!string.IsNullOrWhiteSpace(filter.Area))
            {
                query = query.Where(u => u.Property!.Area == filter.Area);
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
            if (filter.BhkType.HasValue)
            {
                query = query.Where(u => u.BhkType == filter.BhkType.Value);
            }

            var totalCount = await query.CountAsync();

            var units = await query
                .OrderBy(u => u.Property!.Name).ThenBy(u => u.Name)
                .Skip((filter.Page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var propertyIds = units.Select(u => u.PropertyId).Distinct().ToList();
            var ratings = await _context.PropertyReviews
                .Where(r => propertyIds.Contains(r.PropertyId))
                .GroupBy(r => r.PropertyId)
                .Select(g => new { PropertyId = g.Key, Average = g.Average(r => r.Rating), Count = g.Count() })
                .ToDictionaryAsync(g => g.PropertyId);

            var vm = new PublicBrowseViewModel
            {
                Filter = filter,
                Cities = await _locationOptions.GetActiveCitiesAsync(),
                AreasByCity = await _locationOptions.GetActiveAreasByCityAsync(),
                TotalCount = totalCount,
                PageSize = PageSize,
                Units = units.Select(u => new PublicListingViewModel
                {
                    UnitId = u.Id,
                    CoverImageFileName = (u.Images.FirstOrDefault(i => i.IsCover) ?? u.Images.FirstOrDefault())?.FileName,
                    PropertyName = u.Property?.Name ?? string.Empty,
                    Address = u.Property?.Address ?? string.Empty,
                    City = u.Property?.City,
                    Area = u.Property?.Area,
                    Latitude = u.Property?.Latitude,
                    Longitude = u.Property?.Longitude,
                    UnitName = u.Name,
                    UnitType = u.UnitType,
                    BhkType = u.BhkType,
                    MonthlyRent = u.MonthlyRent,
                    Capacity = u.Capacity,
                    BookableSlots = u.BookableSlots,
                    ActiveLeaseCount = u.Leases.Count(l => l.EndDate == null || l.EndDate >= today),
                    AverageRating = ratings.TryGetValue(u.PropertyId, out var r) ? r.Average : null,
                    ReviewCount = ratings.TryGetValue(u.PropertyId, out var rc) ? rc.Count : 0
                }).ToList()
            };

            return View(vm);
        }

        public async Task<IActionResult> Pricing()
        {
            var settings = await _context.AppSettings.FirstOrDefaultAsync() ?? new AppSetting();
            return View(settings);
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
