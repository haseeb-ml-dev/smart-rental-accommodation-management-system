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
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PropertyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var landlordId = _userManager.GetUserId(User)!;

            var properties = await _context.Properties
                .Include(p => p.Units)
                .Where(p => p.LandlordId == landlordId)
                .ToListAsync();

            return View(properties);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PropertyFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PropertyFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var property = new Property
            {
                LandlordId = _userManager.GetUserId(User)!,
                Name = model.Name,
                Address = model.Address
            };

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> AddUnit(int propertyId)
        {
            var property = await GetOwnedPropertyAsync(propertyId);
            if (property == null)
            {
                return NotFound();
            }

            return View(new UnitFormViewModel { PropertyId = property.Id, PropertyName = property.Name });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUnit(UnitFormViewModel model)
        {
            var property = await GetOwnedPropertyAsync(model.PropertyId);
            if (property == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.PropertyName = property.Name;
                return View(model);
            }

            var unit = new Unit
            {
                PropertyId = property.Id,
                Name = model.Name,
                UnitType = model.UnitType,
                MonthlyRent = model.MonthlyRent,
                Capacity = model.Capacity
            };

            _context.Units.Add(unit);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<Property?> GetOwnedPropertyAsync(int propertyId)
        {
            var landlordId = _userManager.GetUserId(User)!;
            return await _context.Properties
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.LandlordId == landlordId);
        }
    }
}
