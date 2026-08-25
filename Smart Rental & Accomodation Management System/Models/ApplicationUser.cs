using Microsoft.AspNetCore.Identity;

namespace Smart_Rental___Accomodation_Management_System.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}
