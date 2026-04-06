using System.ComponentModel.DataAnnotations;

namespace HOA_Management_System.Models.ViewModels
{
    public class HomeownerSettingsViewModel
    {
        public string CurrentUsername { get; set; }

        [Required]
        public string NewUsername { get; set; }

        [Required]
        public string CurrentPassword { get; set; }

        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string? ConfirmNewPassword { get; set; }
    }
}