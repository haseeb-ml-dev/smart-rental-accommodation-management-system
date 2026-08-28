using System.ComponentModel.DataAnnotations;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public class SupportedCity
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
