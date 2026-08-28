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

        // Null EndDate means open-ended (indefinite). A future EndDate means a fixed-term lease that is still
        // active — check IsCurrentlyActive-style logic (EndDate == null || EndDate >= today), never EndDate == null
        // alone, when determining occupancy/billing eligibility.
        public DateTime? EndDate { get; set; }

        public List<RentInvoice> RentInvoices { get; set; } = new();
        public List<LeaseOccupant> Occupants { get; set; } = new();
    }
}
