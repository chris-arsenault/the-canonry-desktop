namespace TheCanonry.Engine.Rules.Types;

using TheCanonry.Engine.Graph;

// =============================================================================
// SELECTION PICK STRATEGY
// =============================================================================

/// <summary>Strategy for picking from selection results.</summary>
public enum SelectionPickStrategy
{
    Random,
    First,
    All,
    Weighted
}

// =============================================================================
// SATURATION LIMIT
// =============================================================================

/// <summary>
/// Filter targets by relationship count. Useful for limiting generator
/// creation based on existing relationships.
/// </summary>
public sealed record SaturationLimit(
    string RelationshipKind,
    Direction Direction,
    string FromKind,
    string FromSubtype,
    int MaxCount);

// =============================================================================
// SELECTION RULE
// =============================================================================

/// <summary>Strategy for finding target entities.</summary>
public enum SelectionStrategy
{
    ByKind,
    ByPreferenceOrder,
    ByRelationship,
    ByProximity,
    ByProminence
}

/// <summary>Rules that determine how to find target entities.</summary>
public sealed record SelectionRule(
    SelectionStrategy Strategy,
    string Kind,
    IReadOnlyList<string> Kinds,
    IReadOnlyList<string> Subtypes,
    IReadOnlyList<string> ExcludeSubtypes,
    string Status,
    IReadOnlyList<string> Statuses,
    string NotStatus,
    string RelationshipKind,
    bool MustHave,
    Direction Direction,
    IReadOnlyList<string> SubtypePreferences,
    string ReferenceEntity,
    double MaxDistance,
    string MinProminence,
    IReadOnlyList<SelectionFilter> Filters,
    IReadOnlyList<SaturationLimit> SaturationLimits,
    SelectionPickStrategy PickStrategy,
    int MaxResults);

// =============================================================================
// RELATED ENTITIES SPEC
// =============================================================================

/// <summary>Specification for selecting entities related to a reference.</summary>
public sealed record RelatedEntitiesSpec(
    string RelatedTo,
    string RelationshipKind,
    Direction Direction);

// =============================================================================
// PATH TRAVERSAL
// =============================================================================

/// <summary>A step in a path traversal for variable selection.</summary>
public sealed record PathTraversalStep(
    string From,
    string Via,
    Direction Direction,
    string TargetKind,
    string TargetSubtype,
    string TargetStatus);

/// <summary>Path-based entity selection via multi-hop traversal.</summary>
public sealed record PathBasedSpec(
    IReadOnlyList<PathTraversalStep> Path);

// =============================================================================
// VARIABLE SELECTION SOURCE
// =============================================================================

/// <summary>
/// Source for variable selection: graph, related entities, or path traversal.
/// </summary>
public abstract record VariableSelectionSource;

public sealed record GraphSource() : VariableSelectionSource;
public sealed record RelatedSource(RelatedEntitiesSpec Spec) : VariableSelectionSource;
public sealed record PathSource(PathBasedSpec Spec) : VariableSelectionSource;

// =============================================================================
// VARIABLE SELECTION RULE
// =============================================================================

/// <summary>Variable selection rule for template variable resolution.</summary>
public sealed record VariableSelectionRule(
    VariableSelectionSource From,
    string Kind,
    IReadOnlyList<string> Kinds,
    IReadOnlyList<string> Subtypes,
    string Status,
    IReadOnlyList<string> Statuses,
    string NotStatus,
    IReadOnlyList<SelectionFilter> Filters,
    IReadOnlyList<SelectionFilter> PreferFilters,
    SelectionPickStrategy PickStrategy,
    int MaxResults);

// =============================================================================
// ENTITY SELECTION CRITERIA
// =============================================================================

/// <summary>Base criteria for filtering entities across contexts.</summary>
public sealed record EntitySelectionCriteria(
    string Kind,
    IReadOnlyList<string> Kinds,
    IReadOnlyList<string> Subtypes,
    IReadOnlyList<string> ExcludeSubtypes,
    string Status,
    IReadOnlyList<string> Statuses,
    string NotStatus,
    string HasTag,
    string NotHasTag);
