using System;
using System.ComponentModel.DataAnnotations; // <--- 1. ADD THIS!

namespace HOA_Management_System.Models
{
    public class DelinquencyHistory
    {
        [Key] // <--- 2. ADD THIS!
        public int HistoryId { get; set; }

        // Links to the exact person who caused the delinquency
        public int MembershipId { get; set; }

        // Links to the property (for quick lookups on the dashboard)
        public int PropertyId { get; set; }

        public DateTime DateDeclared { get; set; }

        // If this is NULL, they are currently delinquent. If it has a date, they are cleared!
        public DateTime? DateResolved { get; set; }

        // --- Navigation Properties ---
        public virtual Membership Membership { get; set; } = null!;
        public virtual Property Property { get; set; } = null!;
    }
}