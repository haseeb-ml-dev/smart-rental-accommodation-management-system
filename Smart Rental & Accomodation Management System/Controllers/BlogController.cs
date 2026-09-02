using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BlogController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var posts = await _context.BlogPosts
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync();

            return View(posts);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string slug)
        {
            var post = await _context.BlogPosts
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var posts = await _context.BlogPosts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new BlogPostFormViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPostFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var post = new BlogPost
            {
                Title = model.Title.Trim(),
                Excerpt = model.Excerpt.Trim(),
                Content = model.Content.Trim(),
                IsPublished = model.IsPublished,
                PublishedAt = model.IsPublished ? DateTime.UtcNow : null
            };

            post.Slug = await BuildUniqueSlugAsync(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);

            _context.BlogPosts.Add(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.BlogPosts.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(new BlogPostFormViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Slug = post.Slug,
                Excerpt = post.Excerpt,
                Content = post.Content,
                IsPublished = post.IsPublished
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BlogPostFormViewModel model)
        {
            var post = await _context.BlogPosts.FirstOrDefaultAsync(p => p.Id == model.Id);
            if (post == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var desiredSlug = string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug;
            if (!string.Equals(desiredSlug.Trim(), post.Slug, StringComparison.OrdinalIgnoreCase))
            {
                post.Slug = await BuildUniqueSlugAsync(desiredSlug, post.Id);
            }

            post.Title = model.Title.Trim();
            post.Excerpt = model.Excerpt.Trim();
            post.Content = model.Content.Trim();

            if (model.IsPublished && !post.IsPublished)
            {
                post.PublishedAt = DateTime.UtcNow;
            }
            post.IsPublished = model.IsPublished;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.BlogPosts.FirstOrDefaultAsync(p => p.Id == id);
            if (post != null)
            {
                _context.BlogPosts.Remove(post);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Manage));
        }

        private async Task<string> BuildUniqueSlugAsync(string source, int? excludeId = null)
        {
            var baseSlug = Slugify(source);
            var slug = baseSlug;
            var suffix = 2;

            while (await _context.BlogPosts.AnyAsync(p => p.Slug == slug && p.Id != excludeId))
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
            return string.IsNullOrEmpty(slug) ? "post" : slug;
        }
    }
}
