using System.ComponentModel.DataAnnotations;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class ProfileFormViewModel
    {
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(150), Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Phone, StringLength(30), Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password), Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6), Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare(nameof(NewPassword)), Display(Name = "Confirm new password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
