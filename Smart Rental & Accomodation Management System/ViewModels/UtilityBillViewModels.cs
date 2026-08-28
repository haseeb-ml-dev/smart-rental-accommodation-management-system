using System.ComponentModel.DataAnnotations;
using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class TenantShareInputViewModel
    {
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal Percentage { get; set; }

        [Range(0, 1000000), Display(Name = "Units consumed")]
        public decimal? UnitsConsumed { get; set; }

        // Visual-only hints for the Create view; server-side filtering is authoritative.
        public bool ExcludedForElectricity { get; set; }
        public bool ExcludedForWater { get; set; }
    }

    public class UtilityBillFormViewModel
    {
        public int PropertyId { get; set; }
        public string? PropertyName { get; set; }

        [Required, Display(Name = "Bill type")]
        public UtilityBillType BillType { get; set; }

        [Range(0.01, 1000000)]
        public decimal Amount { get; set; }

        [Range(1, 12), Display(Name = "Period month")]
        public int PeriodMonth { get; set; } = DateTime.UtcNow.Month;

        [Range(2000, 2100), Display(Name = "Period year")]
        public int PeriodYear { get; set; } = DateTime.UtcNow.Year;

        [Required, Display(Name = "Due date")]
        public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(14);

        [Required, Display(Name = "Split method")]
        public UtilityBillSplitMethod SplitMethod { get; set; } = UtilityBillSplitMethod.Equal;

        [Range(0.01, 10000000), Display(Name = "Total units consumed")]
        public decimal? TotalUnitsConsumed { get; set; }

        public List<TenantShareInputViewModel> Tenants { get; set; } = new();
    }
}
