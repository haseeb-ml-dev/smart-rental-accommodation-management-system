using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class UnitAvailabilityViewModel
    {
        public int UnitId { get; set; }
        public string? CoverImageFileName { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public UnitType UnitType { get; set; }
        public decimal MonthlyRent { get; set; }
        public int Capacity { get; set; }
        public int BookableSlots { get; set; }
        public int ActiveLeaseCount { get; set; }
        public bool IsAvailable => ActiveLeaseCount < BookableSlots;
        public bool HasPendingRequestFromCurrentTenant { get; set; }
    }

    public class UnitDetailViewModel
    {
        public int UnitId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public UnitType UnitType { get; set; }
        public BhkType? BhkType { get; set; }
        public decimal MonthlyRent { get; set; }
        public int Capacity { get; set; }
        public int BookableSlots { get; set; }
        public int ActiveLeaseCount { get; set; }
        public bool IsAvailable => ActiveLeaseCount < BookableSlots;
        public bool HasPendingRequestFromCurrentTenant { get; set; }
        public List<UnitImage> Images { get; set; } = new();
    }

    public class BookingListItemViewModel
    {
        public int BookingId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string? TenantName { get; set; }
        public DateTime StartDate { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
