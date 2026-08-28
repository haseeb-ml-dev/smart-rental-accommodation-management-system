using System.ComponentModel.DataAnnotations;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public enum NotificationType
    {
        [Display(Name = "Rent Due Soon")]
        RentDueSoon,
        [Display(Name = "Rent Overdue")]
        RentOverdue,
        [Display(Name = "Utility Due Soon")]
        UtilityDueSoon,
        [Display(Name = "Utility Overdue")]
        UtilityOverdue,
        [Display(Name = "Dispute Raised")]
        DisputeRaised,
        [Display(Name = "Dispute Resolved")]
        DisputeResolved,
        [Display(Name = "Rent Payment Claimed")]
        RentPaymentClaimed,
        [Display(Name = "Utility Payment Claimed")]
        UtilityPaymentClaimed
    }

    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string RecipientId { get; set; } = string.Empty;
        public ApplicationUser? Recipient { get; set; }

        public NotificationType Type { get; set; }

        [Required, StringLength(300)]
        public string Message { get; set; } = string.Empty;

        public string? LinkController { get; set; }
        public string? LinkAction { get; set; }

        // Invoice or share id this notification is about — used to avoid sending the same reminder twice.
        public int? RelatedEntityId { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
