using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public enum DisputeStatus
    {
        None,
        Open,
        Resolved
    }

    public class UtilityBillShare
    {
        public int Id { get; set; }

        public int UtilityBillId { get; set; }
        public UtilityBill? UtilityBill { get; set; }

        public string TenantId { get; set; } = string.Empty;
        public ApplicationUser? Tenant { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Percentage { get; set; }

        // Only set when the bill's SplitMethod is PerUnitConsumption.
        [Column(TypeName = "decimal(10,2)")]
        public decimal? UnitsConsumed { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ShareAmount { get; set; }

        public bool IsPaid { get; set; }
        public DateTime? PaidDate { get; set; }

        // Set when the tenant claims they've paid, ahead of landlord confirmation via MarkSharePaid.
        public DateTime? TenantMarkedPaidAt { get; set; }
        public string? PaymentSlipFileName { get; set; }

        public DisputeStatus DisputeStatus { get; set; } = DisputeStatus.None;
        [StringLength(500)]
        public string? DisputeReason { get; set; }
        public DateTime? DisputeRaisedAt { get; set; }
        [StringLength(500)]
        public string? DisputeResolution { get; set; }
        public DateTime? DisputeResolvedAt { get; set; }
    }
}
