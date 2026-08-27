using System.ComponentModel.DataAnnotations;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public class Property
    {
        public int Id { get; set; }

        [Required]
        public string LandlordId { get; set; } = string.Empty;
        public ApplicationUser? Landlord { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string Address { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public List<Unit> Units { get; set; } = new();
    }
}
