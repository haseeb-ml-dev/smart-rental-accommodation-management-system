using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    [Authorize]
    public class MessController : Controller
    {
        private static readonly DayOfWeek[] Days =
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };

        private static readonly MealType[] Meals = { MealType.Breakfast, MealType.Lunch, MealType.Dinner };

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Landlord")]
        [HttpGet]
        public async Task<IActionResult> Manage(int propertyId)
        {
            var property = await GetOwnedPropertyAsync(propertyId);
            if (property == null)
            {
                return NotFound();
            }

            var existing = await _context.MessMenus
                .Where(m => m.PropertyId == propertyId)
                .ToListAsync();

            var model = new MessMenuFormViewModel
            {
                PropertyId = property.Id,
                PropertyName = property.Name
            };

            foreach (var day in Days)
            {
                foreach (var meal in Meals)
                {
                    var match = existing.FirstOrDefault(m => m.DayOfWeek == day && m.MealType == meal);
                    model.Entries.Add(new MessMenuEntryViewModel
                    {
                        DayOfWeek = day,
                        MealType = meal,
                        Description = match?.Description ?? string.Empty
                    });
                }
            }

            return View(model);
        }

        [Authorize(Roles = "Landlord")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(MessMenuFormViewModel model)
        {
            var property = await GetOwnedPropertyAsync(model.PropertyId);
            if (property == null)
            {
                return NotFound();
            }

            var existing = await _context.MessMenus
                .Where(m => m.PropertyId == model.PropertyId)
                .ToListAsync();

            foreach (var entry in model.Entries)
            {
                var match = existing.FirstOrDefault(m => m.DayOfWeek == entry.DayOfWeek && m.MealType == entry.MealType);
                var description = entry.Description?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(description))
                {
                    if (match != null)
                    {
                        _context.MessMenus.Remove(match);
                    }
                    continue;
                }

                if (match != null)
                {
                    match.Description = description;
                }
                else
                {
                    _context.MessMenus.Add(new MessMenu
                    {
                        PropertyId = model.PropertyId,
                        DayOfWeek = entry.DayOfWeek,
                        MealType = entry.MealType,
                        Description = description
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage), new { propertyId = model.PropertyId });
        }

        [Authorize(Roles = "Tenant")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tenantId = _userManager.GetUserId(User)!;
            var today = DateTime.UtcNow.Date;

            var lease = await _context.Leases
                .Include(l => l.Unit)
                    .ThenInclude(u => u!.Property)
                .Where(l => l.TenantId == tenantId && (l.EndDate == null || l.EndDate >= today))
                .OrderByDescending(l => l.StartDate)
                .FirstOrDefaultAsync();

            var vm = new MessTenantViewModel();

            if (lease?.Unit?.Property == null)
            {
                return View(vm);
            }

            var propertyId = lease.Unit.Property.Id;
            vm.HasProperty = true;
            vm.PropertyId = propertyId;
            vm.PropertyName = lease.Unit.Property.Name;
            vm.Menu = await BuildMealSlotsAsync(propertyId);
            vm.RecentFeedback = await GetRecentFeedbackAsync(propertyId);

            return View(vm);
        }

        [Authorize(Roles = "Landlord")]
        [HttpGet]
        public async Task<IActionResult> Feedback(int propertyId)
        {
            var property = await GetOwnedPropertyAsync(propertyId);
            if (property == null)
            {
                return NotFound();
            }

            var vm = new MessTenantViewModel
            {
                HasProperty = true,
                PropertyId = property.Id,
                PropertyName = property.Name,
                Menu = await BuildMealSlotsAsync(propertyId),
                RecentFeedback = await GetRecentFeedbackAsync(propertyId)
            };

            return View(vm);
        }

        private async Task<List<MessMealSlotViewModel>> BuildMealSlotsAsync(int propertyId)
        {
            var menuEntries = await _context.MessMenus.Where(m => m.PropertyId == propertyId).ToListAsync();
            var feedback = await _context.MessFeedbacks.Where(f => f.PropertyId == propertyId).ToListAsync();

            var slots = new List<MessMealSlotViewModel>();

            foreach (var day in Days)
            {
                foreach (var meal in Meals)
                {
                    var menu = menuEntries.FirstOrDefault(m => m.DayOfWeek == day && m.MealType == meal);
                    if (menu == null)
                    {
                        continue;
                    }

                    var ratings = feedback.Where(f => f.DayOfWeek == day && f.MealType == meal).Select(f => f.Rating).ToList();

                    slots.Add(new MessMealSlotViewModel
                    {
                        DayOfWeek = day,
                        MealType = meal,
                        Description = menu.Description,
                        AverageRating = ratings.Any() ? ratings.Average() : null,
                        RatingCount = ratings.Count
                    });
                }
            }

            return slots;
        }

        private async Task<List<MessFeedback>> GetRecentFeedbackAsync(int propertyId)
        {
            return await _context.MessFeedbacks
                .Include(f => f.Tenant)
                .Where(f => f.PropertyId == propertyId)
                .OrderByDescending(f => f.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        [Authorize(Roles = "Tenant")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(MessFeedbackFormViewModel model)
        {
            var tenantId = _userManager.GetUserId(User)!;
            var today = DateTime.UtcNow.Date;

            var lease = await _context.Leases
                .Include(l => l.Unit)
                .Where(l => l.TenantId == tenantId && (l.EndDate == null || l.EndDate >= today))
                .OrderByDescending(l => l.StartDate)
                .FirstOrDefaultAsync();

            if (lease?.Unit == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (model.Rating is >= 1 and <= 5)
            {
                _context.MessFeedbacks.Add(new MessFeedback
                {
                    PropertyId = lease.Unit.PropertyId,
                    TenantId = tenantId,
                    DayOfWeek = model.DayOfWeek,
                    MealType = model.MealType,
                    Rating = model.Rating,
                    Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim()
                });
                await _context.SaveChangesAsync();
            }

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
