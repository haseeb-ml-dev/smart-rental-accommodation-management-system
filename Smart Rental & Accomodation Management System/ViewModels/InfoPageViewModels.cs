using System.ComponentModel.DataAnnotations;
using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class InfoPageFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(160), Display(Name = "URL slug (leave blank to auto-generate from the title)")]
        public string? Slug { get; set; }

        [Required, StringLength(300)]
        public string Excerpt { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public InfoPageCategory Category { get; set; }

        [Display(Name = "Published")]
        public bool IsPublished { get; set; }
    }
}
