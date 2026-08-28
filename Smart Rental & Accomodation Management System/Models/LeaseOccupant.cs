using System.ComponentModel.DataAnnotations;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    // A named occupant living under the head tenant's lease — no login of their own.
    // Only meaningful for FamilyUnit leases; the head tenant is the one on the Lease record itself.
    public class LeaseOccupant
    {
        public int Id { get; set; }

        public int LeaseId { get; set; }
        public Lease? Lease { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Phone { get; set; }
    }
}
