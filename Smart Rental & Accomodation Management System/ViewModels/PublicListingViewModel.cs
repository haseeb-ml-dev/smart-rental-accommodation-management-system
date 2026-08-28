using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class PublicListingViewModel
    {
        public int UnitId { get; set; }
        public string? CoverImageFileName { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
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
    }
}
