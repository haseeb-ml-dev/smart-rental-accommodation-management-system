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
    }

    public class UtilityBillFormViewModel
    {
        public int PropertyId { get; set; }
        public string? PropertyName { get; set; }

        [Required]
        public UtilityBillType BillType { get; set; }

        [Range(0.01, 1000000)]
        public decimal Amount { get; set; }

        [Range(1, 12)]
        public int PeriodMonth { get; set; } = DateTime.UtcNow.Month;

        [Range(2000, 2100)]
        public int PeriodYear { get; set; } = DateTime.UtcNow.Year;

        [Required]
        public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(14);

        [Required]
        public UtilityBillSplitMethod SplitMethod { get; set; } = UtilityBillSplitMethod.Equal;

        public List<TenantShareInputViewModel> Tenants { get; set; } = new();
    }
}
