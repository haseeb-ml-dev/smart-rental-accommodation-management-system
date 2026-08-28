using System.ComponentModel.DataAnnotations.Schema;

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

        // Optional — how long the tenant wants the unit for. If approved, this becomes the new Lease's fixed EndDate.
        public DateTime? RequestedEndDate { get; set; }

        // Optional — a counter-offer to the unit's listed MonthlyRent. Informational only; approving does not
        // change the billed rate (negotiation itself is handled outside the system for now).
        [Column(TypeName = "decimal(10,2)")]
        public decimal? ProposedRent { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DecisionDate { get; set; }
    }
}
