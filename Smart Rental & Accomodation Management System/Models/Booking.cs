namespace Smart_Rental___Accomodation_Management_System.Models
{
    public enum BookingStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public class Booking
    {
        public int Id { get; set; }

        public int UnitId { get; set; }
        public Unit? Unit { get; set; }

        public string TenantId { get; set; } = string.Empty;
        public ApplicationUser? Tenant { get; set; }

        public DateTime StartDate { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DecisionDate { get; set; }
    }
}
