using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HOA_Management_System.Models;

[Table("Certification")]
public partial class Certification
{
    [Key]
    [Column("Certification_ID")]
    public int CertificationId { get; set; }

    [Column("Property_ID")]
    public int PropertyId { get; set; }

    [Column("Requestor_Name")]
    public string RequestorName { get; set; } = null!;

    [Column("Cert_Type")]
    public string CertType { get; set; } = null!;

    // Removed DateRequested

    [Column("Date_Issued")]
    public DateOnly? DateIssued { get; set; }

    [Column("Issued_By")]
    public int? IssuedBy { get; set; }

    [Column("Fee_Amount")]
    public decimal FeeAmount { get; set; }

    [ForeignKey("IssuedBy")]
    public virtual UserAccount? IssuedByNavigation { get; set; }

    [ForeignKey("PropertyId")]
    public virtual Property? Property { get; set; } = null!;
}