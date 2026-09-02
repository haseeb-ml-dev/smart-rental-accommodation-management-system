using System.ComponentModel.DataAnnotations;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public class BlogPost
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(160)]
        public string Slug { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string Excerpt { get; set; } = string.Empty;

        // Plain text; paragraphs are separated by a blank line and wrapped in <p> at render time.
        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
