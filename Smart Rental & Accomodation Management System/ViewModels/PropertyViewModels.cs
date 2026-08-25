using System.ComponentModel.DataAnnotations;
using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class PropertyFormViewModel
    {
        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string Address { get; set; } = string.Empty;
    }

    public class UnitFormViewModel
    {
        public int PropertyId { get; set; }
        public string? PropertyName { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public UnitType UnitType { get; set; }

        [Range(0, 100000)]
        public decimal MonthlyRent { get; set; }

        [Range(1, 20)]
        public int Capacity { get; set; } = 1;
    }
}
