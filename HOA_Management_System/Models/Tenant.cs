using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HOA_Management_System.Models
{
    public class Tenant
    {
        [Key]
        public int TenantId { get; set; } // Maps to "Tenant_ID" in DB

        public int PropertyId { get; set; } // Maps to "Property_ID" in DB

        [ForeignKey("PropertyId")]
        public Property? Property { get; set; }

        [Display(Name = "Full Name")]
        public string? FullName { get; set; } // Maps to "Full_Name" in DB

        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; } // Maps to "Contact_Number" in DB

        public string? Status { get; set; }

        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; } // Maps to "Start_Date" in DB

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; } // Maps to "End_Date" in DB
    }
}