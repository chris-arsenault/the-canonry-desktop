namespace TheCanonry.Engine.Templates;

using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

// =============================================================================
// PLACEMENT DEBUG
// =============================================================================

/// <summary>Debug information about entity placement.</summary>
public sealed record PlacementDebug(
    string AnchorType,
    EntityId? AnchorEntityId,
    string AnchorEntityName,
    string AnchorEntityKind,
    string AnchorCulture,
    string ResolvedVia,
    IReadOnlyList<string> SeedRegionsAvailable,
    string? EmergentRegionId,
    string? EmergentRegionLabel,
    string? RegionId,
    IReadOnlyList<string> AllRegionIds);

// =============================================================================
// TEMPLATE RESULT
// =============================================================================

/// <summary>
/// Result of template expansion. Captures all entities, relationships,
/// and state updates produced by a single template application.
/// </summary>
public sealed record TemplateResult(
    /// <summary>Entities created by the template.</summary>
    IReadOnlyList<Entity> Entities,

    /// <summary>Relationships created by the template.</summary>
    IReadOnlyList<Relationship> Relationships,

    /// <summary>Human-readable description of what the template did.</summary>
    string Description,

    /// <summary>Placement strategies used (parallel to Entities array, for debugging).</summary>
    IReadOnlyList<string> PlacementStrategies,

    /// <summary>Tags derived from placement per entity (parallel to Entities array).</summary>
    IReadOnlyList<IReadOnlyDictionary<string, object>> DerivedTagsList,

    /// <summary>Detailed placement debug info per entity.</summary>
    IReadOnlyList<PlacementDebug> PlacementDebugList,

    /// <summary>
    /// Resolved context variables from template execution.
    /// Passed to growthSystem for narration generation AFTER entities have names.
    /// Keys include $target, $enemy, etc.
    /// </summary>
    IReadOnlyDictionary<string, object?> ResolvedVariables,

    /// <summary>
    /// Maps entityRef (like "$spell") to index in Entities array.
    /// Needed because CreateChance can skip entities, misaligning indices.
    /// </summary>
    IReadOnlyDictionary<string, int> EntityRefToIndex)
{
    public static TemplateResult Empty { get; } = new(
        [],
        [],
        "",
        [],
        [],
        [],
        new Dictionary<string, object?>(),
        new Dictionary<string, int>());
}
