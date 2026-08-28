using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.Services
{
    public class BillingService
    {
        private readonly ApplicationDbContext _context;

        public BillingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task GenerateMonthlyInvoicesAsync()
        {
            var today = DateTime.UtcNow.Date;

            var activeLeases = await _context.Leases
                .Include(l => l.Unit)
                .Where(l => l.EndDate == null)
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
                .Where(i => i.Status == InvoiceStatus.Pending && i.DueDate < today)
                .ToListAsync();

            foreach (var invoice in overdueInvoices)
            {
                invoice.Status = InvoiceStatus.Overdue;

                var lease = invoice.Lease!;
                var tenantName = lease.Tenant?.FullName ?? "A tenant";
                var period = $"{invoice.PeriodMonth}/{invoice.PeriodYear}";

                _context.Notifications.Add(new Notification
                {
                    RecipientId = lease.TenantId,
                    Type = NotificationType.RentOverdue,
                    Message = $"Your rent for {period} ({invoice.Amount:C}) is now overdue.",
                    LinkController = "Tenant",
                    LinkAction = "Index",
                    RelatedEntityId = invoice.Id
                });

                var landlordId = lease.Unit?.Property?.LandlordId;
                if (landlordId != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        RecipientId = landlordId,
                        Type = NotificationType.RentOverdue,
                        Message = $"{tenantName}'s rent for {period} ({invoice.Amount:C}) is now overdue.",
                        LinkController = "Landlord",
                        LinkAction = "OverdueTenants",
                        RelatedEntityId = invoice.Id
                    });
                }
            }

            if (overdueInvoices.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task SendRentDueSoonRemindersAsync()
        {
            var today = DateTime.UtcNow.Date;
            var horizon = today.AddDays(3);

            var upcomingInvoices = await _context.RentInvoices
                .Include(i => i.Lease)
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

                _context.Notifications.Add(new Notification
                {
                    RecipientId = invoice.Lease!.TenantId,
                    Type = NotificationType.RentDueSoon,
                    Message = $"Rent of {invoice.Amount:C} is due on {invoice.DueDate:MMM d, yyyy}.",
                    LinkController = "Tenant",
                    LinkAction = "Index",
                    RelatedEntityId = invoice.Id
                });
            }

            if (upcomingInvoices.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task SendUtilityRemindersAsync()
        {
            var today = DateTime.UtcNow.Date;
            var horizon = today.AddDays(3);

            var unpaidShares = await _context.UtilityBillShares
                .Include(s => s.UtilityBill)
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
            }

            if (unpaidShares.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
