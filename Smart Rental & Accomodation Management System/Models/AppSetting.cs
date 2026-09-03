using System.ComponentModel.DataAnnotations.Schema;

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

        // Flat monthly subscription fee per landlord account, shown on the Subscription page.
        [Column(TypeName = "decimal(10,2)")]
        public decimal MonthlySubscriptionFee { get; set; } = 999m;

        // Real bank/JazzCash/EasyPaisa payment details, shown verbatim on the Subscription page.
        // Left blank until an admin fills in the real details — never fabricated by the app.
        public string? SubscriptionPaymentInstructions { get; set; }
    }
}
