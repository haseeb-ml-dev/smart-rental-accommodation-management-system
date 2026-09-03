using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public enum SubscriptionClaimStatus
    {
        Pending,
        Confirmed,
        Rejected
    }

    public class SubscriptionClaim
    {
        public int Id { get; set; }

        [Required]
        public string LandlordId { get; set; } = string.Empty;
        public ApplicationUser? Landlord { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public DateTime ClaimedAt { get; set; } = DateTime.UtcNow;
        public string? ProofFileName { get; set; }

        public SubscriptionClaimStatus Status { get; set; } = SubscriptionClaimStatus.Pending;
        public DateTime? ConfirmedAt { get; set; }

        [StringLength(500)]
        public string? AdminNote { get; set; }
    }
}
