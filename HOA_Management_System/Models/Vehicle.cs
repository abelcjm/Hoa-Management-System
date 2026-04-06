using System;
using System.Collections.Generic;

namespace HOA_Management_System.Models;

public partial class Vehicle
{
    public int VehicleId { get; set; }

    public int PropertyId { get; set; }

    public string VehicleOwner { get; set; } = null!;

    public string? DriverName { get; set; }

    public string VehicleType { get; set; } = null!;

    public string? Make { get; set; }

    public string? Model { get; set; }

    public string? Year { get; set; }

    public string? Color { get; set; }

    public string PlateNumber { get; set; } = null!;

    public string? StickerNumber { get; set; }

    public int StickerYear { get; set; }

    public virtual Property Property { get; set; } = null!;
}
