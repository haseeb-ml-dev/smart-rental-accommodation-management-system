using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class MessMenuEntryViewModel
    {
        public DayOfWeek DayOfWeek { get; set; }
        public MealType MealType { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class MessMenuFormViewModel
    {
        public int PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public List<MessMenuEntryViewModel> Entries { get; set; } = new();
    }

    public class MessMealSlotViewModel
    {
        public DayOfWeek DayOfWeek { get; set; }
        public MealType MealType { get; set; }
        public string Description { get; set; } = string.Empty;
        public double? AverageRating { get; set; }
        public int RatingCount { get; set; }
    }

    public class MessTenantViewModel
    {
        public bool HasProperty { get; set; }
        public string? PropertyName { get; set; }
        public int PropertyId { get; set; }
        public List<MessMealSlotViewModel> Menu { get; set; } = new();
        public List<MessFeedback> RecentFeedback { get; set; } = new();
    }

    public class MessFeedbackFormViewModel
    {
        public DayOfWeek DayOfWeek { get; set; }
        public MealType MealType { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class PropertyMessRatingViewModel
    {
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string LandlordName { get; set; } = string.Empty;
        public int FeedbackCount { get; set; }
        public double AverageRating { get; set; }
        public List<MessFeedback> RecentFeedback { get; set; } = new();
    }
}
