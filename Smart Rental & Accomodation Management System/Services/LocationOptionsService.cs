using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;

namespace Smart_Rental___Accomodation_Management_System.Services
{
    // Central place for every "what cities/areas can be picked" query. Two different audiences
    // need this: the public/tenant Browse filters (derived from cities/areas actually in use on
    // active listings) and the landlord's Property Create/Edit form (derived from the admin-managed
    // SupportedCities/SupportedAreas lists). Kept in one service so the query isn't duplicated
    // across HomeController, BookingController and PropertyController.
    public class LocationOptionsService
    {
        private readonly ApplicationDbContext _context;

        public LocationOptionsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetActiveCitiesAsync()
        {
            return await _context.Properties
                .Where(p => p.IsActive && p.City != null)
                .Select(p => p.City!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<Dictionary<string, List<string>>> GetActiveAreasByCityAsync()
        {
            var rows = await _context.Properties
                .Where(p => p.IsActive && p.City != null && p.Area != null)
                .Select(p => new { City = p.City!, Area = p.Area! })
                .Distinct()
                .ToListAsync();

            return rows
                .GroupBy(r => r.City)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Area).OrderBy(a => a).ToList());
        }

        public async Task<List<string>> GetSupportedCityNamesAsync()
        {
            return await _context.SupportedCities
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();
        }

        public async Task<Dictionary<string, List<string>>> GetSupportedAreasByCityAsync()
        {
            var rows = await _context.SupportedAreas
                .Include(a => a.SupportedCity)
                .Where(a => a.SupportedCity != null)
                .ToListAsync();

            return rows
                .GroupBy(a => a.SupportedCity!.Name)
                .ToDictionary(g => g.Key, g => g.Select(a => a.Name).OrderBy(n => n).ToList());
        }
    }
}
