using System.ComponentModel.DataAnnotations;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public enum MaintenanceCategory
    {
        Plumbing,
        Electrical,
        Appliance,
        Structural,
        PestControl,
        Other
    }

    public enum MaintenancePriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    public enum MaintenanceStatus
    {
        Open,
        InProgress,
        Resolved
    }

    public class MaintenanceRequest
    {
        public int Id { get; set; }

        public int UnitId { get; set; }
        public Unit? Unit { get; set; }

        [Required]
        public string TenantId { get; set; } = string.Empty;
        public ApplicationUser? Tenant { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public MaintenanceCategory Category { get; set; }
        public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;

        public string? PhotoFileName { get; set; }

        [StringLength(500)]
        public string? LandlordNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
