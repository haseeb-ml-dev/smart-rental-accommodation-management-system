namespace Smart_Rental___Accomodation_Management_System.Models
{
    public enum MealType
    {
        Breakfast,
        Lunch,
        Dinner
    }

    public class MessMenu
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public Property? Property { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
        public MealType MealType { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
