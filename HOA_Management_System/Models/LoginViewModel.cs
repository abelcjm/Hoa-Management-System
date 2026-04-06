using System.ComponentModel.DataAnnotations;

namespace HOA_Management_System.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email/Username is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}