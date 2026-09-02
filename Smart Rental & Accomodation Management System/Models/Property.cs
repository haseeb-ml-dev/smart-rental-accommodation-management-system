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

        // Optional — picked from the admin-managed supported-cities list (Admin/Settings) when one is configured.
        [StringLength(100)]
        public string? City { get; set; }

        // Optional — picked from the admin-managed supported-areas list for the chosen City when one is configured.
        [StringLength(100)]
        public string? Area { get; set; }

        // Populated by GeocodingService from Address; null if geocoding hasn't run yet, failed, or no API key is configured.
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public bool IsActive { get; set; } = true;

        public List<Unit> Units { get; set; } = new();
    }
}
