using System.ComponentModel.DataAnnotations;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public class Lease
    {
        public int Id { get; set; }

        public int UnitId { get; set; }
        public Unit? Unit { get; set; }

        [Required]
        public string TenantId { get; set; } = string.Empty;
        public ApplicationUser? Tenant { get; set; }

        public DateTime StartDate { get; set; }

        // Null EndDate means the lease is currently active.
        public DateTime? EndDate { get; set; }

        public List<RentInvoice> RentInvoices { get; set; } = new();
        public List<LeaseOccupant> Occupants { get; set; } = new();
    }
}
