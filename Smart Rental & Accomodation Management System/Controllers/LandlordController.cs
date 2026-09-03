using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.Services;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    [Authorize(Roles = "Landlord")]
    public class LandlordController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ReportExportService _reportExportService;

        public LandlordController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ReportExportService reportExportService)
        {
            _context = context;
            _userManager = userManager;
            _reportExportService = reportExportService;
        }

        public async Task<IActionResult> Index()
        {
            var landlordId = _userManager.GetUserId(User)!;
            var today = DateTime.UtcNow.Date;
            var landlordUser = await _userManager.GetUserAsync(User);
            var now = DateTime.UtcNow;

            var units = await _context.Units
                .Include(u => u.Property)
                .Include(u => u.Leases)
                .Where(u => u.Property!.LandlordId == landlordId)
                .ToListAsync();

            var unitIds = units.Select(u => u.Id).ToHashSet();

            var activeLeaseUnitIds = units
                .SelectMany(u => u.Leases)
                .Where(l => l.EndDate == null || l.EndDate >= today)
                .Select(l => l.UnitId)
                .ToHashSet();

            var invoices = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Unit)
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Tenant)
                .Where(i => unitIds.Contains(i.Lease!.UnitId))
                .OrderByDescending(i => i.DueDate)
                .ToListAsync();

            var vm = new LandlordDashboardViewModel
            {
                TotalProperties = units.Select(u => u.PropertyId).Distinct().Count(),
                TotalUnits = units.Count,
                OccupiedUnits = activeLeaseUnitIds.Count,
                VacantUnits = units.Count - activeLeaseUnitIds.Count,
                ActiveTenants = units.SelectMany(u => u.Leases).Where(l => l.EndDate == null || l.EndDate >= today).Select(l => l.TenantId).Distinct().Count(),
                CollectedThisMonth = invoices.Where(i => i.DueDate.Year == today.Year && i.DueDate.Month == today.Month && i.Status == InvoiceStatus.Paid).Sum(i => i.Amount),
                OutstandingThisMonth = invoices.Where(i => i.DueDate.Year == today.Year && i.DueDate.Month == today.Month && i.Status != InvoiceStatus.Paid).Sum(i => i.Amount),
                OverdueInvoiceCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue),
                RecentInvoices = invoices.Take(10).ToList()
            };

            for (int monthsAgo = 5; monthsAgo >= 0; monthsAgo--)
            {
                var monthDate = new DateTime(today.Year, today.Month, 1).AddMonths(-monthsAgo);
                var monthInvoices = invoices.Where(i => i.DueDate.Year == monthDate.Year && i.DueDate.Month == monthDate.Month).ToList();

                vm.MonthlyCollection.Add(new MonthlyCollectionPoint
                {
                    Label = monthDate.ToString("MMM yyyy"),
                    Collected = monthInvoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount),
                    Outstanding = monthInvoices.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => i.Amount)
                });
            }

            vm.OutstandingUtilities = await _context.UtilityBillShares
                .Where(s => !s.IsPaid && s.UtilityBill!.Property!.LandlordId == landlordId)
                .SumAsync(s => s.ShareAmount);

            vm.OpenMaintenanceRequestCount = await _context.MaintenanceRequests
                .CountAsync(m => m.Unit!.Property!.LandlordId == landlordId && m.Status != MaintenanceStatus.Resolved);

            vm.IsTrialActive = landlordUser?.TrialEndsAt.HasValue == true && landlordUser.TrialEndsAt.Value > now;
            vm.IsSubscriptionActive = landlordUser?.SubscriptionActiveUntil.HasValue == true && landlordUser.SubscriptionActiveUntil.Value > now;
            vm.TrialEndsAt = landlordUser?.TrialEndsAt;

            return View(vm);
        }

        public async Task<IActionResult> OverdueTenants()
        {
            var landlordId = _userManager.GetUserId(User)!;

            var overdueInvoices = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Unit)
                        .ThenInclude(u => u!.Property)
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Tenant)
                .Where(i => i.Status == InvoiceStatus.Overdue && i.Lease!.Unit!.Property!.LandlordId == landlordId)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var overdueShares = await _context.UtilityBillShares
                .Include(s => s.UtilityBill)
                    .ThenInclude(b => b!.Property)
                .Include(s => s.Tenant)
                .Where(s => !s.IsPaid && s.UtilityBill!.DueDate < today && s.UtilityBill.Property!.LandlordId == landlordId)
                .ToListAsync();

            var groups = new List<OverdueTenantGroupViewModel>();

            foreach (var invoiceGroup in overdueInvoices.GroupBy(i => i.Lease!.TenantId))
            {
                var first = invoiceGroup.First();
                groups.Add(new OverdueTenantGroupViewModel
                {
                    TenantName = first.Lease!.Tenant!.FullName,
                    PropertyName = first.Lease.Unit!.Property!.Name,
                    UnitName = first.Lease.Unit.Name,
                    OverdueInvoices = invoiceGroup.OrderBy(i => i.DueDate).ToList(),
                    OverdueUtilityShares = overdueShares.Where(s => s.TenantId == invoiceGroup.Key).ToList()
                });
            }

            var invoiceTenantIds = overdueInvoices.Select(i => i.Lease!.TenantId).ToHashSet();
            foreach (var shareGroup in overdueShares.Where(s => !invoiceTenantIds.Contains(s.TenantId)).GroupBy(s => s.TenantId))
            {
                var first = shareGroup.First();
                groups.Add(new OverdueTenantGroupViewModel
                {
                    TenantName = first.Tenant!.FullName,
                    PropertyName = first.UtilityBill!.Property!.Name,
                    UnitName = string.Empty,
                    OverdueUtilityShares = shareGroup.ToList()
                });
            }

            return View(groups.OrderByDescending(g => g.TotalOverdue).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkInvoicePaid(int invoiceId)
        {
            var landlordId = _userManager.GetUserId(User)!;

            var invoice = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Unit)
                        .ThenInclude(u => u!.Property)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Lease!.Unit!.Property!.LandlordId == landlordId);

            if (invoice == null)
            {
                return NotFound();
            }

            if (invoice.Status != InvoiceStatus.Paid)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = DateTime.UtcNow;

                _context.Notifications.Add(new Notification
                {
                    RecipientId = invoice.Lease!.TenantId,
                    Type = NotificationType.RentPaymentConfirmed,
                    Message = $"Your {invoice.PeriodMonth}/{invoice.PeriodYear} rent payment ({invoice.Amount:C}) was confirmed received.",
                    LinkController = "Tenant",
                    LinkAction = "Index",
                    RelatedEntityId = invoice.Id
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectInvoicePayment(int invoiceId)
        {
            var landlordId = _userManager.GetUserId(User)!;

            var invoice = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Unit)
                        .ThenInclude(u => u!.Property)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Lease!.Unit!.Property!.LandlordId == landlordId);

            if (invoice == null)
            {
                return NotFound();
            }

            if (invoice.TenantMarkedPaidAt != null && invoice.DisputeStatus == DisputeStatus.None)
            {
                invoice.TenantMarkedPaidAt = null;
                invoice.PaymentSlipFileName = null;
                invoice.DisputeStatus = DisputeStatus.Open;
                invoice.DisputeReason = "Landlord indicated this payment was not received.";
                invoice.DisputeRaisedAt = DateTime.UtcNow;

                _context.Notifications.Add(new Notification
                {
                    RecipientId = invoice.Lease!.TenantId,
                    Type = NotificationType.DisputeRaised,
                    Message = $"Your landlord said your {invoice.PeriodMonth}/{invoice.PeriodYear} rent payment ({invoice.Amount:C}) was not received.",
                    LinkController = "Tenant",
                    LinkAction = "Index",
                    RelatedEntityId = invoice.Id
                });

                await _context.SaveChangesAsync();
                TempData["Message"] = "Marked as not received. The tenant has been notified.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveInvoiceDispute(int invoiceId, string resolution, decimal? adjustedAmount)
        {
            var landlordId = _userManager.GetUserId(User)!;

            var invoice = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Unit)
                        .ThenInclude(u => u!.Property)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Lease!.Unit!.Property!.LandlordId == landlordId);

            if (invoice == null)
            {
                return NotFound();
            }

            if (invoice.DisputeStatus == DisputeStatus.Open)
            {
                invoice.DisputeStatus = DisputeStatus.Resolved;
                invoice.DisputeResolution = string.IsNullOrWhiteSpace(resolution) ? null : resolution.Trim();
                invoice.DisputeResolvedAt = DateTime.UtcNow;

                if (adjustedAmount is > 0)
                {
                    invoice.Amount = adjustedAmount.Value;
                }

                _context.Notifications.Add(new Notification
                {
                    RecipientId = invoice.Lease!.TenantId,
                    Type = NotificationType.DisputeResolved,
                    Message = $"Your rent dispute was resolved: {invoice.DisputeResolution ?? "no additional notes"}.",
                    LinkController = "Tenant",
                    LinkAction = "Index",
                    RelatedEntityId = invoice.Id
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportExcel()
        {
            var data = await BuildReportDataAsync();
            var bytes = _reportExportService.BuildExcel(data);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"landlord-report-{DateTime.UtcNow:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportPdf()
        {
            var data = await BuildReportDataAsync();
            var bytes = _reportExportService.BuildPdf(data);
            return File(bytes, "application/pdf", $"landlord-report-{DateTime.UtcNow:yyyyMMdd}.pdf");
        }

        private async Task<LandlordReportData> BuildReportDataAsync()
        {
            var landlordId = _userManager.GetUserId(User)!;
            var landlord = await _userManager.GetUserAsync(User);
            var today = DateTime.UtcNow.Date;

            var units = await _context.Units
                .Include(u => u.Property)
                .Include(u => u.Leases)
                .Where(u => u.Property!.LandlordId == landlordId)
                .ToListAsync();

            var unitIds = units.Select(u => u.Id).ToHashSet();

            var activeLeaseUnitIds = units
                .SelectMany(u => u.Leases)
                .Where(l => l.EndDate == null || l.EndDate >= today)
                .Select(l => l.UnitId)
                .ToHashSet();

            var invoices = await _context.RentInvoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Unit)
                        .ThenInclude(u => u!.Property)
                .Include(i => i.Lease)
                    .ThenInclude(l => l!.Tenant)
                .Where(i => unitIds.Contains(i.Lease!.UnitId))
                .OrderByDescending(i => i.DueDate)
                .ToListAsync();

            var data = new LandlordReportData
            {
                LandlordName = landlord?.FullName ?? "Landlord",
                TotalProperties = units.Select(u => u.PropertyId).Distinct().Count(),
                TotalUnits = units.Count,
                OccupiedUnits = activeLeaseUnitIds.Count,
                VacantUnits = units.Count - activeLeaseUnitIds.Count,
                ActiveTenants = units.SelectMany(u => u.Leases).Where(l => l.EndDate == null || l.EndDate >= today).Select(l => l.TenantId).Distinct().Count(),
                CollectedThisMonth = invoices.Where(i => i.DueDate.Year == today.Year && i.DueDate.Month == today.Month && i.Status == InvoiceStatus.Paid).Sum(i => i.Amount),
                OutstandingThisMonth = invoices.Where(i => i.DueDate.Year == today.Year && i.DueDate.Month == today.Month && i.Status != InvoiceStatus.Paid).Sum(i => i.Amount),
                OverdueInvoiceCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue)
            };

            data.PropertyOccupancy = units
                .GroupBy(u => u.Property!.Name)
                .Select(g => new PropertyOccupancyRow
                {
                    PropertyName = g.Key,
                    TotalUnits = g.Count(),
                    OccupiedUnits = g.Count(u => activeLeaseUnitIds.Contains(u.Id))
                })
                .OrderBy(p => p.PropertyName)
                .ToList();

            data.Invoices = invoices
                .Select(i => new InvoiceReportRow
                {
                    PropertyName = i.Lease?.Unit?.Property?.Name ?? string.Empty,
                    UnitName = i.Lease?.Unit?.Name ?? string.Empty,
                    TenantName = i.Lease?.Tenant?.FullName ?? string.Empty,
                    PeriodMonth = i.PeriodMonth,
                    PeriodYear = i.PeriodYear,
                    Amount = i.Amount,
                    DueDate = i.DueDate,
                    Status = i.Status,
                    PaidDate = i.PaidDate
                })
                .ToList();

            for (int monthsAgo = 5; monthsAgo >= 0; monthsAgo--)
            {
                var monthDate = new DateTime(today.Year, today.Month, 1).AddMonths(-monthsAgo);
                var monthInvoices = invoices.Where(i => i.DueDate.Year == monthDate.Year && i.DueDate.Month == monthDate.Month).ToList();

                data.MonthlyCollection.Add(new MonthlyCollectionPoint
                {
                    Label = monthDate.ToString("MMM yyyy"),
                    Collected = monthInvoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount),
                    Outstanding = monthInvoices.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => i.Amount)
                });
            }

            data.OutstandingUtilities = await _context.UtilityBillShares
                .Where(s => !s.IsPaid && s.UtilityBill!.Property!.LandlordId == landlordId)
                .SumAsync(s => s.ShareAmount);

            return data;
        }
    }
}
