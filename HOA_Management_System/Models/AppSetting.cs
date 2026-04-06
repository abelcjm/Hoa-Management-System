using System.ComponentModel.DataAnnotations;

namespace HOA_Management_System.Models
{
    public class AppSetting
    {
        [Key]
        public string Key { get; set; } = null!; // e.g., "MonthlyFee"
        public string Value { get; set; } = null!; // e.g., "500.00"
    }
}