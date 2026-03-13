using TheCanonry.Schema.Ids;

namespace TheCanonry.Engine.Engine;

// =============================================================================
// ACTION CONTEXT
// =============================================================================

/// <summary>
/// Attribution context for narrative events.
/// Used by systems like UniversalCatalyst to attribute modifications
/// to specific actions rather than the generic system.
/// </summary>
public sealed record ActionContext(
    string Source,
    string SourceId,
    bool Success);

// =============================================================================
// SYSTEM RESULT DETAIL RECORDS
// =============================================================================

/// <summary>A relationship added by a system.</summary>
public sealed record RelationshipAdded(
    string Kind,
    string Src,
    string Dst,
    double Strength,
    string Category,
    string CatalyzedBy,
    int? CreatedAt,
    ActionContext ActionContext,
    string NarrativeGroupId);

/// <summary>A relationship strength adjustment by a system.</summary>
public sealed record RelationshipAdjusted(
    string Kind,
    string Src,
    string Dst,
    double Delta,
    ActionContext ActionContext,
    string NarrativeGroupId);

/// <summary>A relationship to be archived (deferred until WorldEngine applies).</summary>
public sealed record RelationshipToArchive(
    string Kind,
    string Src,
    string Dst,
    ActionContext ActionContext,
    string NarrativeGroupId);

/// <summary>An entity modified by a system.</summary>
public sealed record EntityModified(
    EntityId Id,
    IReadOnlyDictionary<string, object?> Changes,
    ActionContext ActionContext,
    string NarrativeGroupId);

// =============================================================================
// SYSTEM RESULT
// =============================================================================

/// <summary>
/// Result of a simulation system's Apply method.
/// Captures all side effects for the engine to process.
/// </summary>
public sealed record SystemResult(
    IReadOnlyList<RelationshipAdded> RelationshipsAdded,
    IReadOnlyList<RelationshipAdjusted> RelationshipsAdjusted,
    IReadOnlyList<RelationshipToArchive> RelationshipsToArchive,
    IReadOnlyList<EntityModified> EntitiesModified,
    IReadOnlyDictionary<string, double> PressureChanges,
    string Description,
    IReadOnlyDictionary<string, object?> Details,
    IReadOnlyDictionary<string, string> NarrationsByGroup)
{
    public static SystemResult Empty { get; } = new(
        [],
        [],
        [],
        [],
        new Dictionary<string, double>(),
        "",
        new Dictionary<string, object?>(),
        new Dictionary<string, string>());
}
