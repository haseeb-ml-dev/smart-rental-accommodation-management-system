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

        public List<SupportedCity> Cities { get; set; } = new();
    }
}
