using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public enum UtilityBillType
    {
        Electricity,
        Water,
        Internet,
        Other
    }

    public enum UtilityBillSplitMethod
    {
        Equal,
        [Display(Name = "Custom Percentage")]
        CustomPercentage,
        [Display(Name = "Per Unit Consumption")]
        PerUnitConsumption
    }

    public class UtilityBill
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public Property? Property { get; set; }

        public UtilityBillType BillType { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public DateTime DueDate { get; set; }

        public UtilityBillSplitMethod SplitMethod { get; set; }

        // Only set when SplitMethod is PerUnitConsumption — the meter reading total the bill amount is based on.
        [Column(TypeName = "decimal(10,2)")]
        public decimal? TotalUnitsConsumed { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<UtilityBillShare> Shares { get; set; } = new();
    }
}
