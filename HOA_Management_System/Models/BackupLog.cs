using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HOA_Management_System.Models
{
    [Table("BackupLog")]
    public class BackupLog
    {
        [Key]
        public int Backup_ID { get; set; }

        [Required]
        [Column(TypeName = "varchar(255)")]
        public string Backup_File_Name { get; set; }

        [Required]
        public DateTime Backup_Date { get; set; }

        public int? Performed_By { get; set; }

        [ForeignKey("Performed_By")]
        public virtual UserAccount? UserAccount { get; set; } 

        [Required]
        [Column(TypeName = "varchar(20)")]
        public string Backup_Type { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string? Notes { get; set; }
    }
}