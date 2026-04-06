using System;
using System.Collections.Generic;

namespace HOA_Management_System.Models;

public partial class Membership
{
    public int MembershipId { get; set; }

    public int PropertyId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string? CivilStatus { get; set; }

    public string? Sex { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? SpouseName { get; set; }

    public string ContactNumber { get; set; } = null!;

    public string? EmailAddress { get; set; }

    public int? YearsStay { get; set; }

    public bool IsVoter { get; set; }

    public string? PreferredPayment { get; set; }

    public int? CountSeniors { get; set; }

    public int? CountHelpers { get; set; }

    public int? CountChildren { get; set; }

    public int? CountOthers { get; set; }

    public int? TotalMembers { get; set; }

    public int? CountDogs { get; set; }

    public bool? IsDogLeashed { get; set; }

    public int? CountCats { get; set; }

    public bool? IsCatLeashed { get; set; }

    public string? OtherPets { get; set; }

    // --- NEW OWNERSHIP TRACKING COLUMNS ---
    public bool IsCurrentOwner { get; set; } = true;
    public DateOnly? MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }

    public virtual Property Property { get; set; } = null!;
}