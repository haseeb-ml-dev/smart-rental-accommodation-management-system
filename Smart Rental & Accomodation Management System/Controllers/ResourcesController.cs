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

        public ResourcesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pages = await _context.InfoPages
                .Where(p => p.IsPublished)
                .OrderBy(p => p.Category)
                    .ThenBy(p => p.Title)
                .ToListAsync();

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

            return View(page);
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
