using System;
using System.Collections.Generic;

namespace HOA_Management_System.Models;

public partial class AuditLog
{
    public long AuditId { get; set; }

    public string ActionType { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public int RecordId { get; set; }

    public int UserId { get; set; }

    public DateTime Timestamp { get; set; }

    public string? Details { get; set; }

    public virtual UserAccount User { get; set; } = null!;
}
