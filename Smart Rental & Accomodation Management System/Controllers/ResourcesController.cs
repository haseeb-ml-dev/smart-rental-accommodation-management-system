using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    public class ResourcesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ResourcesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Every seeded feature page has a hand-drawn illustration at wwwroot/images/features/{slug}.svg;
        // a page created later through Admin > Manage falls back to a generic one instead of a broken image.
        private string ResolveHeroImage(string slug)
        {
            var path = Path.Combine(_environment.WebRootPath, "images", "features", $"{slug}.svg");
            return System.IO.File.Exists(path) ? $"/images/features/{slug}.svg" : "/images/features/default.svg";
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pages = await _context.InfoPages
                .Where(p => p.IsPublished)
                .OrderBy(p => p.Category)
                    .ThenBy(p => p.Title)
                .ToListAsync();

            if (User.IsInRole("Landlord"))
            {
                pages = pages.Where(p => p.Category is InfoPageCategory.ForLandlords or InfoPageCategory.HowItWorks).ToList();
            }
            else if (User.IsInRole("Tenant"))
            {
                pages = pages.Where(p => p.Category is InfoPageCategory.ForTenants or InfoPageCategory.HowItWorks).ToList();
            }

            ViewBag.HeroImages = pages.ToDictionary(p => p.Slug, p => ResolveHeroImage(p.Slug));

            return View(pages);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string slug)
        {
            var page = await _context.InfoPages
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

            if (page == null)
            {
                return NotFound();
            }

            var cta = GetFeatureCta(page.Slug);
            ViewBag.CtaController = cta?.Controller;
            ViewBag.CtaAction = cta?.Action;
            ViewBag.CtaText = cta?.Text;
            ViewBag.HeroImage = ResolveHeroImage(page.Slug);

            return View(page);
        }

        private (string Controller, string Action, string Text)? GetFeatureCta(string slug)
        {
            var isLandlord = User.IsInRole("Landlord");
            var isTenant = User.IsInRole("Tenant");
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

            return slug switch
            {
                "for-landlords" => isLandlord
                    ? ("Landlord", "Index", "Go to your dashboard")
                    : !isAuthenticated ? ("Account", "Register", "Get started as a landlord") : null,

                "for-tenants" => isTenant
                    ? ("Tenant", "Index", "Go to your dashboard")
                    : !isAuthenticated ? ("Account", "Register", "Get started as a tenant") : null,

                "how-it-works" => isLandlord ? ("Landlord", "Index", "Go to your dashboard")
                    : isTenant ? ("Booking", "Browse", "Browse & book a place")
                    : !isAuthenticated ? ("Account", "Register", "Get started") : null,

                "property-listings" => isLandlord
                    ? ("Property", "Index", "Manage your properties")
                    : ("Home", "Listings", "Browse listings"),

                "rent-collection" => isLandlord ? ("Landlord", "Index", "Go to your dashboard")
                    : isTenant ? ("Tenant", "Index", "Go to your dashboard")
                    : ("Account", "Register", "Get started as a landlord"),

                "utility-bill-splitting" => isLandlord ? ("UtilityBill", "Index", "Manage utility bills")
                    : isTenant ? ("Tenant", "Index", "Go to your dashboard")
                    : ("Account", "Register", "Get started"),

                "maintenance-tracking" => isLandlord ? ("Maintenance", "Index", "View maintenance requests")
                    : isTenant ? ("Maintenance", "MyRequests", "Report an issue")
                    : ("Account", "Register", "Get started"),

                "search-and-booking" => isTenant ? ("Booking", "Browse", "Browse & book a place")
                    : isLandlord ? null : ("Home", "Listings", "Browse listings"),

                "rent-payments" => isTenant ? ("Tenant", "Index", "Go to your dashboard")
                    : isLandlord ? ("Landlord", "Index", "Go to your dashboard")
                    : ("Account", "Register", "Get started as a tenant"),

                "reviews-and-ratings" => isTenant ? ("Review", "MyReviews", "View your reviews")
                    : isLandlord ? null : ("Home", "Listings", "Browse listings"),

                _ => null
            };
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var pages = await _context.InfoPages
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(pages);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new InfoPageFormViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InfoPageFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var page = new InfoPage
            {
                Title = model.Title.Trim(),
                Excerpt = model.Excerpt.Trim(),
                Content = model.Content.Trim(),
                Category = model.Category,
                IsPublished = model.IsPublished,
                PublishedAt = model.IsPublished ? DateTime.UtcNow : null
            };

            page.Slug = await BuildUniqueSlugAsync(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);

            _context.InfoPages.Add(page);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var page = await _context.InfoPages.FirstOrDefaultAsync(p => p.Id == id);
            if (page == null)
            {
                return NotFound();
            }

            return View(new InfoPageFormViewModel
            {
                Id = page.Id,
                Title = page.Title,
                Slug = page.Slug,
                Excerpt = page.Excerpt,
                Content = page.Content,
                Category = page.Category,
                IsPublished = page.IsPublished
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InfoPageFormViewModel model)
        {
            var page = await _context.InfoPages.FirstOrDefaultAsync(p => p.Id == model.Id);
            if (page == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var desiredSlug = string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug;
            if (!string.Equals(desiredSlug.Trim(), page.Slug, StringComparison.OrdinalIgnoreCase))
            {
                page.Slug = await BuildUniqueSlugAsync(desiredSlug, page.Id);
            }

            page.Title = model.Title.Trim();
            page.Excerpt = model.Excerpt.Trim();
            page.Content = model.Content.Trim();
            page.Category = model.Category;

            if (model.IsPublished && !page.IsPublished)
            {
                page.PublishedAt = DateTime.UtcNow;
            }
            page.IsPublished = model.IsPublished;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var page = await _context.InfoPages.FirstOrDefaultAsync(p => p.Id == id);
            if (page != null)
            {
                _context.InfoPages.Remove(page);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Manage));
        }

        private async Task<string> BuildUniqueSlugAsync(string source, int? excludeId = null)
        {
            var baseSlug = Slugify(source);
            var slug = baseSlug;
            var suffix = 2;

            while (await _context.InfoPages.AnyAsync(p => p.Slug == slug && p.Id != excludeId))
            {
                slug = $"{baseSlug}-{suffix}";
                suffix++;
            }

            return slug;
        }

        private static string Slugify(string value)
        {
            var sb = new StringBuilder();
            var lastWasHyphen = false;

            foreach (var c in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lastWasHyphen = false;
                }
                else if (!lastWasHyphen && sb.Length > 0)
                {
                    sb.Append('-');
                    lastWasHyphen = true;
                }
            }

            var slug = sb.ToString().Trim('-');
            if (slug.Length > 140)
            {
                slug = slug[..140].Trim('-');
            }
            return string.IsNullOrEmpty(slug) ? "page" : slug;
        }
    }
}
