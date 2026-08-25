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
                .Where(i => i.Status == InvoiceStatus.Pending && i.DueDate < today)
                .ToListAsync();

            foreach (var invoice in overdueInvoices)
            {
                invoice.Status = InvoiceStatus.Overdue;
            }

            if (overdueInvoices.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
