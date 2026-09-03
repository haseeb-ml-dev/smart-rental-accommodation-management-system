using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class SubscriptionStatusViewModel
    {
        public bool IsTrialActive { get; set; }
        public bool IsSubscriptionActive { get; set; }
        public DateTime? TrialEndsAt { get; set; }
        public DateTime? SubscriptionActiveUntil { get; set; }
        public bool IsEntitled => IsTrialActive || IsSubscriptionActive;

        public decimal MonthlyFee { get; set; }
        public string? PaymentInstructions { get; set; }

        public SubscriptionClaim? PendingClaim { get; set; }
        public List<SubscriptionClaim> History { get; set; } = new();
    }

    public class SubscriptionClaimRowViewModel
    {
        public int Id { get; set; }
        public string LandlordName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ClaimedAt { get; set; }
        public string? ProofFileName { get; set; }
        public SubscriptionClaimStatus Status { get; set; }
    }
}
