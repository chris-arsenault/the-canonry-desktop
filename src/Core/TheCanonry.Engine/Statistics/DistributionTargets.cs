namespace TheCanonry.Engine.Statistics;

/// <summary>
/// Represents target entity counts from domain configuration.
/// Used to define desired population levels for homeostatic regulation.
/// </summary>
public class DistributionTargets
{
    /// <summary>
    /// Entity targets: kind -> subtype -> target count.
    /// </summary>
    public Dictionary<string, Dictionary<string, int>> Entities { get; init; } = [];

    /// <summary>
    /// Get the target count for a specific kind:subtype.
    /// Returns 0 if no target is defined.
    /// </summary>
    public int GetTarget(string kind, string subtype)
    {
        if (Entities.TryGetValue(kind, out var subtypes) &&
            subtypes.TryGetValue(subtype, out var target))
            return target;
        return 0;
    }
}
