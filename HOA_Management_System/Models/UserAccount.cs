using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HOA_Management_System.Models
{
    [Table("UserAccount")]
    public class UserAccount
    {
        [Key]
        [Column("User_ID")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [NotMapped] 
        public string Password { get; set; }

        [Column("Password_Hash")]
        [MaxLength(255)]
        public string? PasswordHash { get; set; }

        // --- THE CRITICAL PART ---
        // Ensure 'MembershipId' IS NOT HERE. Only 'PropertyId' should exist.
        [Column("PropertyId")]
        public int? PropertyId { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string Role { get; set; } = "Homeowner";

        [Column("Is_Active")]
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public string Status
        {
            get { return IsActive ? "Active" : "Inactive"; }
            set { IsActive = (value == "Active"); }
        }

        public DateTime? Last_Login { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Certification> Certifications { get; set; } = new List<Certification>();

        // This connects to the Property table
        public virtual Property? Property { get; set; }
    }
}