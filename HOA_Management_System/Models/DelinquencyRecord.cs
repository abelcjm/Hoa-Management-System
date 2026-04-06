using System.ComponentModel.DataAnnotations;

namespace HOA_Management_System.Models
{
    public class DelinquencyRecord
    {
        [Key]
        public int Id { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal DelinquencyRate { get; set; } // The % score
        public int TotalHomeowners { get; set; }
        public int DelinquentCount { get; set; } // Count of people > 6 months behind
        public bool IsFinalized { get; set; } = false;
    }
}