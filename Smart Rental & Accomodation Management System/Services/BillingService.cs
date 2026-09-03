using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.Services
{
    public class BillingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public BillingService(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task GenerateMonthlyInvoicesAsync()
        {
            var today = DateTime.UtcNow.Date;

            var activeLeases = await _context.Leases
                .Include(l => l.Unit)
                .Where(l => l.StartDate <= today && (l.EndDate == null || l.EndDate >= today))
                .ToListAsync();

            foreach (var lease in activeLeases)
            {
                var hasCurrentInvoice = await _context.RentInvoices.AnyAsync(i =>
                    i.LeaseId == lease.Id && i.PeriodMonth == today.Month && i.PeriodYear == today.Year);

                if (hasCurrentInvoice)
                {
                    continue;
                }

                _context.RentInvoices.Add(new RentInvoice
                {
                    LeaseId = lease.Id,
                    PeriodMonth = today.Month,
                    PeriodYear = today.Year,
                    Amount = lease.Unit?.MonthlyRent ?? 0,
                    DueDate = new DateTime(today.Year, today.Month, 5),
                    Status = InvoiceStatus.Pending
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task FlagOverdueInvoicesAsync()
        {
            var today = DateTime.UtcNow.Date;

            var overdueInvoices = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Tenant)
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Unit)
                        .ThenInclude(u => u!.Property)
                            .ThenInclude(p => p!.Landlord)
                .Where(i => i.Status == InvoiceStatus.Pending && i.DueDate < today)
                .ToListAsync();

            foreach (var invoice in overdueInvoices)
            {
                invoice.Status = InvoiceStatus.Overdue;

                var lease = invoice.Lease!;
                var tenantName = lease.Tenant?.FullName ?? "A tenant";
                var period = $"{invoice.PeriodMonth}/{invoice.PeriodYear}";

                var tenantMessage = $"Your rent for {period} ({invoice.Amount:C}) is now overdue.";
                _context.Notifications.Add(new Notification
                {
                    RecipientId = lease.TenantId,
                    Type = NotificationType.RentOverdue,
                    Message = tenantMessage,
                    LinkController = "Tenant",
                    LinkAction = "Index",
                    RelatedEntityId = invoice.Id
                });
                await TrySendEmailAsync(lease.Tenant, "Rent overdue", tenantMessage);

                var landlord = lease.Unit?.Property?.Landlord;
                if (landlord != null)
                {
                    var landlordMessage = $"{tenantName}'s rent for {period} ({invoice.Amount:C}) is now overdue.";
                    _context.Notifications.Add(new Notification
                    {
                        RecipientId = landlord.Id,
                        Type = NotificationType.RentOverdue,
                        Message = landlordMessage,
                        LinkController = "Landlord",
                        LinkAction = "OverdueTenants",
                        RelatedEntityId = invoice.Id
                    });
                    await TrySendEmailAsync(landlord, "Tenant rent overdue", landlordMessage);
                }
            }

            if (overdueInvoices.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task SendRentDueSoonRemindersAsync()
        {
            var settings = await GetSettingsAsync();
            var today = DateTime.UtcNow.Date;
            var horizon = today.AddDays(settings.RentReminderDaysBefore);

            var upcomingInvoices = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Tenant)
                .Where(i => i.Status == InvoiceStatus.Pending && i.DueDate >= today && i.DueDate <= horizon)
                .ToListAsync();

            foreach (var invoice in upcomingInvoices)
            {
                var alreadySent = await _context.Notifications.AnyAsync(n =>
                    n.Type == NotificationType.RentDueSoon && n.RelatedEntityId == invoice.Id);

                if (alreadySent)
                {
                    continue;
                }

                var message = $"Rent of {invoice.Amount:C} is due on {invoice.DueDate:MMM d, yyyy}.";

                _context.Notifications.Add(new Notification
                {
                    RecipientId = invoice.Lease!.TenantId,
                    Type = NotificationType.RentDueSoon,
                    Message = message,
                    LinkController = "Tenant",
                    LinkAction = "Index",
                    RelatedEntityId = invoice.Id
                });
                await TrySendEmailAsync(invoice.Lease!.Tenant, "Rent due soon", message);
            }

            if (upcomingInvoices.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task SendUtilityRemindersAsync()
        {
            var settings = await GetSettingsAsync();
            var today = DateTime.UtcNow.Date;
            var horizon = today.AddDays(settings.UtilityReminderDaysBefore);

            var unpaidShares = await _context.UtilityBillShares
                .Include(s => s.UtilityBill)
                .Include(s => s.Tenant)
                .Where(s => !s.IsPaid && s.UtilityBill!.DueDate <= horizon)
                .ToListAsync();

            foreach (var share in unpaidShares)
            {
                var isOverdue = share.UtilityBill!.DueDate < today;
                var type = isOverdue ? NotificationType.UtilityOverdue : NotificationType.UtilityDueSoon;

                var alreadySent = await _context.Notifications.AnyAsync(n =>
                    n.Type == type && n.RelatedEntityId == share.Id);

                if (alreadySent)
                {
                    continue;
                }

                var message = isOverdue
                    ? $"Your utility share of {share.ShareAmount:C} is now overdue."
                    : $"Your utility share of {share.ShareAmount:C} is due on {share.UtilityBill.DueDate:MMM d, yyyy}.";

                _context.Notifications.Add(new Notification
                {
                    RecipientId = share.TenantId,
                    Type = type,
                    Message = message,
                    LinkController = "Tenant",
                    LinkAction = "Index",
                    RelatedEntityId = share.Id
                });
                await TrySendEmailAsync(share.Tenant, isOverdue ? "Utility bill overdue" : "Utility bill due soon", message);
            }

            if (unpaidShares.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        // A missing/unconfigured email address must never break the reminder pass — the in-app
        // notification above already covers it, and IEmailSender itself logs instead of throwing
        // when no SMTP host is configured. Also skips entirely when the recipient has opted out
        // via Account/Settings — the in-app notification still went out regardless.
        private async Task TrySendEmailAsync(ApplicationUser? recipient, string subject, string message)
        {
            if (recipient == null || !recipient.EmailNotificationsEnabled || string.IsNullOrWhiteSpace(recipient.Email))
            {
                return;
            }

            await _emailSender.SendEmailAsync(recipient.Email, subject, message);
        }

        // Falls back to the model defaults if the singleton settings row is somehow missing —
        // SeedData creates it on startup, but a reminder pass must never throw over this.
        private async Task<AppSetting> GetSettingsAsync()
        {
            return await _context.AppSettings.FirstOrDefaultAsync() ?? new AppSetting();
        }
    }
}
