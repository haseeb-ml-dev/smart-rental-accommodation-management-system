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
        public string? Area { get; set; }
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
        public double? AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class PublicBrowseViewModel
    {
        public UnitSearchFilter Filter { get; set; } = new();
        public List<PublicListingViewModel> Units { get; set; } = new();
        public List<string> Cities { get; set; } = new();
        public Dictionary<string, List<string>> AreasByCity { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
