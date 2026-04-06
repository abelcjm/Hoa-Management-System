using System.ComponentModel.DataAnnotations;

namespace HOA_Management_System.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string PropertyInfo { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your new password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }

    public class SystemConfigViewModel
    {
        // Default Rate Display
        public decimal MonthlyFee { get; set; }

        // Manual Generation Range
        public int StartMonth { get; set; } = 1;
        public int StartYear { get; set; } = DateTime.Now.Year;

        public int EndMonth { get; set; } = 12;
        public int EndYear { get; set; } = DateTime.Now.Year;

        public decimal TargetAmount { get; set; } = 500;
    }

    public class SettingsIndexViewModel
    {
        public UserProfileViewModel Profile { get; set; }
        public ChangePasswordViewModel Password { get; set; }
        public SystemConfigViewModel System { get; set; }
        public string ActiveTab { get; set; } = "profile";
    }
}