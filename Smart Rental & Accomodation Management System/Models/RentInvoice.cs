using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public enum InvoiceStatus
    {
        Pending,
        Paid,
        Overdue
    }

    public class RentInvoice
    {
        public int Id { get; set; }

        public int LeaseId { get; set; }
        public Lease? Lease { get; set; }

        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

        public DateTime? PaidDate { get; set; }

        // Set when the tenant claims they've paid, ahead of landlord confirmation via MarkInvoicePaid.
        public DateTime? TenantMarkedPaidAt { get; set; }
        public string? PaymentSlipFileName { get; set; }
    }
}
