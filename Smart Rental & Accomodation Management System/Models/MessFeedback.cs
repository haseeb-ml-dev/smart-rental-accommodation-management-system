namespace Smart_Rental___Accomodation_Management_System.Models
{
    public class MessFeedback
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public Property? Property { get; set; }

        public string TenantId { get; set; } = string.Empty;
        public ApplicationUser? Tenant { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
        public MealType MealType { get; set; }

        public int Rating { get; set; }
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
