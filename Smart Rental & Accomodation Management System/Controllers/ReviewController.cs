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
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> MyReviews()
        {
            var tenantId = _userManager.GetUserId(User)!;

            var properties = await _context.Leases
                .Where(l => l.TenantId == tenantId)
                .Select(l => new { l.Unit!.PropertyId, l.Unit.Property!.Name })
                .Distinct()
                .ToListAsync();

            var existingReviews = await _context.PropertyReviews
                .Where(r => r.TenantId == tenantId)
                .ToListAsync();

            var vm = properties
                .Select(p =>
                {
                    var existing = existingReviews.FirstOrDefault(r => r.PropertyId == p.PropertyId);
                    return new ReviewablePropertyViewModel
                    {
                        PropertyId = p.PropertyId,
                        PropertyName = p.Name,
                        ExistingRating = existing?.Rating,
                        ExistingComment = existing?.Comment
                    };
                })
                .OrderBy(p => p.PropertyName)
                .ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int propertyId, int rating, string? comment)
        {
            var tenantId = _userManager.GetUserId(User)!;

            var eligible = await _context.Leases
                .AnyAsync(l => l.TenantId == tenantId && l.Unit!.PropertyId == propertyId);

            if (!eligible)
            {
                return NotFound();
            }

            if (rating is < 1 or > 5)
            {
                TempData["Message"] = "Rating must be between 1 and 5.";
                return RedirectToAction(nameof(MyReviews));
            }

            var review = await _context.PropertyReviews
                .FirstOrDefaultAsync(r => r.PropertyId == propertyId && r.TenantId == tenantId);

            var trimmedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

            if (review == null)
            {
                _context.PropertyReviews.Add(new PropertyReview
                {
                    PropertyId = propertyId,
                    TenantId = tenantId,
                    Rating = rating,
                    Comment = trimmedComment
                });
            }
            else
            {
                review.Rating = rating;
                review.Comment = trimmedComment;
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Thanks for the review.";

            return RedirectToAction(nameof(MyReviews));
        }
    }
}
