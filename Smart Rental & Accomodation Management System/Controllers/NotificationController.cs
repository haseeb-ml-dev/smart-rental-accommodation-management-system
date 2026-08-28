using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            var notifications = await _context.Notifications
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            // Snapshot which were unread before marking them read, so this page load can still highlight what's new.
            var wasUnread = notifications.Where(n => !n.IsRead).Select(n => n.Id).ToHashSet();

            foreach (var notification in notifications.Where(n => !n.IsRead))
            {
                notification.IsRead = true;
            }

            if (wasUnread.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            ViewBag.WasUnread = wasUnread;
            return View(notifications);
        }
    }
}
