namespace Smart_Rental___Accomodation_Management_System.Models
{
    // Singleton row (always Id = 1) holding platform-wide configuration. Read by BillingService and
    // UtilityBillController instead of the hardcoded defaults they used before.
    public class AppSetting
    {
        public int Id { get; set; }

        public UtilityBillSplitMethod DefaultUtilitySplitMethod { get; set; } = UtilityBillSplitMethod.Equal;

        public int RentReminderDaysBefore { get; set; } = 3;
        public int UtilityReminderDaysBefore { get; set; } = 3;
    }
}
