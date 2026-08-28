using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public enum UnitType
    {
        [Display(Name = "Private Room")]
        PrivateRoom,
        [Display(Name = "Shared Room")]
        SharedRoom,
        [Display(Name = "Family Unit")]
        FamilyUnit
    }

    public enum BhkType
    {
        Studio,
        OneBHK,
        TwoBHK,
        ThreeBHK,
        FourBHK,
        FivePlusBHK
    }

    public class Unit
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public Property? Property { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public UnitType UnitType { get; set; }

        public BhkType? BhkType { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MonthlyRent { get; set; }

        // Occupancy estimate (e.g. "sleeps 6") for PrivateRoom/FamilyUnit; seat count for SharedRoom.
        public int Capacity { get; set; } = 1;

        public bool HasIndividualElectricityMeter { get; set; }
        public bool HasIndividualWaterMeter { get; set; }

        // Only SharedRoom supports multiple concurrent bookings; a PrivateRoom/FamilyUnit is one booking, period.
        [NotMapped]
        public int BookableSlots => UnitType == UnitType.SharedRoom ? Capacity : 1;

        public List<Lease> Leases { get; set; } = new();
        public List<Booking> Bookings { get; set; } = new();
    }
}
