using System;
using System.Collections.Generic;

namespace HOA_Management_System.Models;

public partial class MonthlyDue
{
    public int BillId { get; set; }

    public int PropertyId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal AmountDue { get; set; }

    public decimal? TotalPaid { get; set; }

    public DateOnly? DateFullyPaid { get; set; }

    public string Status { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;
}
