using System;
using System.Collections.Generic;

namespace HOA_Management_System.Models;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public int PropertyId { get; set; }

    public string OrNumber { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateOnly PaymentDate { get; set; }

    public string PaymentType { get; set; } = null!;

    public int EncodedBy { get; set; }

    public string? Notes { get; set; }

    public virtual UserAccount EncodedByNavigation { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;
}
