namespace TheCanonry.Engine.Coordinates;

/// <summary>
/// Options for entity placement on a semantic plane.
/// </summary>
public class PlacementOptions
{
    /// <summary>Minimum distance from existing entities on the plane.</summary>
    public double MinDistance { get; init; } = 5.0;

    /// <summary>Whether to allow emergent region creation when seed regions are full.</summary>
    public bool AllowEmergent { get; init; } = true;

    /// <summary>When true, bias region selection toward sparser regions.</summary>
    public bool PreferSparse { get; init; }

    /// <summary>Maximum sampling attempts before giving up.</summary>
    public int MaxAttempts { get; init; } = 20;
}
