namespace TheCanonry.Engine.Templates;

// =============================================================================
// SUBTYPE SPEC
// =============================================================================

/// <summary>How to determine the subtype of a created entity.</summary>
public abstract record SubtypeSpec;

/// <summary>Fixed subtype value.</summary>
public sealed record FixedSubtype(string Value) : SubtypeSpec;

/// <summary>Inherit subtype from a reference entity with a given probability.</summary>
public sealed record InheritSubtype(string Ref, double Chance) : SubtypeSpec;

/// <summary>Choose subtype based on dominant pressure.</summary>
public sealed record FromPressureSubtype(
    IReadOnlyDictionary<string, string> PressureMapping) : SubtypeSpec;

/// <summary>Pick a random subtype from a list.</summary>
public sealed record RandomSubtype(IReadOnlyList<string> Options) : SubtypeSpec;

/// <summary>Conditionally select subtype based on world state.</summary>
public sealed record ConditionalSubtype(
    IReadOnlyList<SubtypeWhen> When,
    string Otherwise) : SubtypeSpec;

/// <summary>A single conditional branch for subtype selection.</summary>
public sealed record SubtypeWhen(
    SubtypeCondition Condition,
    string Then);

/// <summary>Condition for conditional subtype selection.</summary>
public abstract record SubtypeCondition;

/// <summary>Check if target entity has a specific subtype.</summary>
public sealed record TargetSubtypeCondition(string EqualsSubtype) : SubtypeCondition;

/// <summary>Check pressure threshold.</summary>
public sealed record PressureCheckCondition(
    string PressureId,
    double Min,
    double Max) : SubtypeCondition;

// =============================================================================
// CULTURE SPEC
// =============================================================================

/// <summary>How to determine the culture of a created entity.</summary>
public abstract record CultureSpec;

/// <summary>Inherit culture from a reference entity.</summary>
public sealed record InheritCulture(string Ref) : CultureSpec;

/// <summary>Use a fixed culture.</summary>
public sealed record FixedCulture(string Id) : CultureSpec;

// =============================================================================
// DESCRIPTION SPEC
// =============================================================================

/// <summary>How to generate the description of a created entity.</summary>
public abstract record DescriptionSpec;

/// <summary>Fixed description text.</summary>
public sealed record FixedDescription(string Value) : DescriptionSpec;

/// <summary>Template-based description with variable replacements.</summary>
public sealed record TemplateDescription(
    string Template,
    IReadOnlyDictionary<string, string> Replacements) : DescriptionSpec;

// =============================================================================
// COUNT RANGE
// =============================================================================

/// <summary>Range for batch entity creation.</summary>
public readonly record struct CountRange(int Min, int Max);

// =============================================================================
// CREATION RULE
// =============================================================================

/// <summary>
/// Rules that define how to create new entities within a template.
/// </summary>
public sealed record CreationRule(
    /// <summary>Variable name for this entity (e.g., "$newFaction", "$child").</summary>
    string EntityRef,

    /// <summary>What entity kind to create.</summary>
    string Kind,

    /// <summary>How to determine the subtype.</summary>
    SubtypeSpec Subtype,

    /// <summary>Initial entity status.</summary>
    string Status,

    /// <summary>Initial prominence label (template uses label; interpreter converts to numeric).</summary>
    string Prominence,

    /// <summary>How to determine the culture.</summary>
    CultureSpec Culture,

    /// <summary>How to generate the description.</summary>
    DescriptionSpec Description,

    /// <summary>Initial boolean tags to set on the entity.</summary>
    IReadOnlyDictionary<string, bool> Tags,

    /// <summary>Placement specification.</summary>
    PlacementSpec Placement,

    /// <summary>Number of entities to create (single value or range for batch).</summary>
    CountRange Count,

    /// <summary>
    /// Optional creation chance (0.0 to 1.0).
    /// If less than 1.0, entity is only created with the given probability.
    /// Useful for cross-pollination (e.g., 25% chance to also create an artifact).
    /// </summary>
    double CreateChance);
