using System.ComponentModel.DataAnnotations;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public class SupportedArea
    {
        public int Id { get; set; }

        [Required]
        public int SupportedCityId { get; set; }
        public SupportedCity? SupportedCity { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
