using Microsoft.AspNetCore.Identity;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        // Landlord-only: set at registration, gives 30 days of free access before Property/Create
        // requires an active subscription. Null for Tenant/Admin accounts.
        public DateTime? TrialEndsAt { get; set; }

        // Landlord-only: extended by 30 days each time an Admin confirms a SubscriptionClaim.
        public DateTime? SubscriptionActiveUntil { get; set; }

        // Controls whether reminder emails are sent; in-app notifications always continue regardless.
        public bool EmailNotificationsEnabled { get; set; } = true;
    }
}
