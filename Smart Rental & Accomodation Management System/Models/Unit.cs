using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public enum UnitType
    {
        PrivateRoom,
        SharedRoom,
        FamilyUnit
    }

    public class Unit
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public Property? Property { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public UnitType UnitType { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MonthlyRent { get; set; }

        public List<Lease> Leases { get; set; } = new();
    }
}
