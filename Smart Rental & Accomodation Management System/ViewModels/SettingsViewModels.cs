using System.ComponentModel.DataAnnotations;
using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class SettingsFormViewModel
    {
        [Display(Name = "Default utility split method")]
        public UtilityBillSplitMethod DefaultUtilitySplitMethod { get; set; }

        [Range(0, 30), Display(Name = "Rent due-soon reminder (days before due date)")]
        public int RentReminderDaysBefore { get; set; }

        [Range(0, 30), Display(Name = "Utility due-soon reminder (days before due date)")]
        public int UtilityReminderDaysBefore { get; set; }

        [Range(0, 100000), Display(Name = "Monthly subscription fee (per landlord account)")]
        public decimal MonthlySubscriptionFee { get; set; }

        [StringLength(1000), Display(Name = "Subscription payment instructions (shown to landlords)")]
        public string? SubscriptionPaymentInstructions { get; set; }

        public List<CityWithAreasViewModel> Cities { get; set; } = new();
    }

    public class CityWithAreasViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<SupportedArea> Areas { get; set; } = new();
    }
}
