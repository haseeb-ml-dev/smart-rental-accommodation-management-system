using System.ComponentModel.DataAnnotations;
using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class MaintenanceRequestFormViewModel
    {
        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public MaintenanceCategory Category { get; set; }

        [Display(Name = "Priority")]
        public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;

        public IFormFile? Photo { get; set; }
    }

    public class MaintenanceRequestListItemViewModel
    {
        public int Id { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string? TenantName { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MaintenanceCategory Category { get; set; }
        public MaintenancePriority Priority { get; set; }
        public MaintenanceStatus Status { get; set; }
        public string? PhotoFileName { get; set; }
        public string? LandlordNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
