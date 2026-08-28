using System.ComponentModel.DataAnnotations;
using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class PropertyFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string Address { get; set; } = string.Empty;
    }

    public class UnitFormViewModel
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public string? PropertyName { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, Display(Name = "Unit type")]
        public UnitType UnitType { get; set; }

        [Display(Name = "BHK")]
        public BhkType? BhkType { get; set; }

        [Range(0, 100000), Display(Name = "Monthly rent")]
        public decimal MonthlyRent { get; set; }

        [Range(1, 20)]
        public int Capacity { get; set; } = 1;

        [Display(Name = "Has its own electricity meter")]
        public bool HasIndividualElectricityMeter { get; set; }

        [Display(Name = "Has its own water meter")]
        public bool HasIndividualWaterMeter { get; set; }
    }

    public class ActiveLeaseRowViewModel
    {
        public int LeaseId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
    }

    public class TenantOptionViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class UnitTenantsViewModel
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;

        public int BookableSlots { get; set; }
        public int ActiveLeaseCount { get; set; }
        public bool HasFreeSlot => ActiveLeaseCount < BookableSlots;

        public List<ActiveLeaseRowViewModel> ActiveLeases { get; set; } = new();
        public List<TenantOptionViewModel> AvailableTenants { get; set; } = new();
    }
}
