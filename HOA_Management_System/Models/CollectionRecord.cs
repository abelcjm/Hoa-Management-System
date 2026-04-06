using System.ComponentModel.DataAnnotations;

namespace HOA_Management_System.Models
{
    public class CollectionRecord
    {
        [Key]
        public int Id { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        // The Snapshot Data
        public decimal TotalTarget { get; set; }     // How much we SHOULD have collected
        public decimal TotalCollected { get; set; }  // How much CASH we actually got that month
        public decimal EfficiencyRate { get; set; }  // The % score

        public bool IsFinalized { get; set; } = false; // Safety check
    }
}