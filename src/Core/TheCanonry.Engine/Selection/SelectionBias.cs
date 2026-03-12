namespace TheCanonry.Engine.Selection;

/// <summary>
/// Configuration for weighted target selection.
/// Defines preferences (boost score) and penalties (reduce score) for candidate entities.
/// </summary>
public class SelectionBias
{
    public SelectionPreferences Prefer { get; init; } = new();
    public SelectionAvoidance Avoid { get; init; } = new();
    public CultureRequirement Culture { get; init; } = new();
}

/// <summary>
/// Positive preferences: boost score for entities matching these attributes.
/// </summary>
public class SelectionPreferences
{
    /// <summary>Preferred subtypes (e.g., ["merchant", "outlaw"]).</summary>
    public List<string> Subtypes { get; init; } = [];

    /// <summary>Preferred tags (e.g., ["mystic", "explorer"]).</summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>Preferred prominence labels (e.g., ["Recognized", "Renowned"]).</summary>
    public List<string> Prominence { get; init; } = [];

    /// <summary>Boost multiplier for preferred attributes (default: 1.5).</summary>
    public double PreferenceBoost { get; init; } = 1.5;
}

/// <summary>
/// Negative penalties: reduce score for oversaturated entities.
/// </summary>
public class SelectionAvoidance
{
    /// <summary>Relationship kinds to penalize (e.g., ["member_of"]).</summary>
    public List<string> RelationshipKinds { get; init; } = [];

    /// <summary>Exponential hub penalty strength (default: 0.1).</summary>
    public double HubPenaltyStrength { get; init; } = 0.1;

    /// <summary>Hard cap: never select entities with this many total relationships. 0 = no limit.</summary>
    public int MaxTotalRelationships { get; init; } = 50;
}

/// <summary>
/// Hard culture filters applied before scoring.
/// </summary>
public class CultureRequirement
{
    /// <summary>Only select entities with this exact culture. Null = no requirement.</summary>
    public string? Require { get; init; }

    /// <summary>Exclude entities with any of these cultures.</summary>
    public List<string> Exclude { get; init; } = [];
}
