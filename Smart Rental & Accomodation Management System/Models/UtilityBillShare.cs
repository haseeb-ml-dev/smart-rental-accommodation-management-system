using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Rental___Accomodation_Management_System.Models
{
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
    }
}
