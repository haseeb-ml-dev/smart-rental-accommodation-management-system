namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class ReviewablePropertyViewModel
    {
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public int? ExistingRating { get; set; }
        public string? ExistingComment { get; set; }
    }

    public class PropertyReviewRowViewModel
    {
        public string TenantName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
