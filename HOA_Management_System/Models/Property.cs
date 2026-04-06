using System;
using System.Collections.Generic;

namespace HOA_Management_System.Models;

public partial class Property
{
    public int PropertyId { get; set; }

    public string Block { get; set; } = null!;

    public string Lot { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateOnly DateRegistered { get; set; }

    public virtual ICollection<Certification> Certifications { get; set; } = new List<Certification>();

    public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();

    public virtual ICollection<MonthlyDue> MonthlyDues { get; set; } = new List<MonthlyDue>();

    public virtual ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    public virtual ICollection<UserAccount> UserAccounts { get; set; } = new List<UserAccount>();
}
