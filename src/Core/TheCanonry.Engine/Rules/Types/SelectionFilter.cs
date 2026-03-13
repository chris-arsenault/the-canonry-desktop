using TheCanonry.Engine.Graph;

namespace TheCanonry.Engine.Rules.Types;

/// <summary>
/// Abstract base for all selection filters. Filters determine whether
/// an entity should be included in a selection result set.
/// </summary>
public abstract record SelectionFilter;

// =============================================================================
// EXCLUDE FILTER
// =============================================================================

/// <summary>Exclude entities matching variable references.</summary>
public sealed record ExcludeEntitiesFilter(
    IReadOnlyList<string> Entities) : SelectionFilter;

// =============================================================================
// RELATIONSHIP FILTERS
// =============================================================================

/// <summary>Entity must have a relationship of the given kind.</summary>
public sealed record HasRelationshipFilter(
    string Kind,
    string With,
    Direction Direction) : SelectionFilter;

/// <summary>Entity must lack a relationship of the given kind.</summary>
public sealed record LacksRelationshipFilter(
    string Kind,
    string With) : SelectionFilter;

/// <summary>Entity shares a common related entity with a reference.</summary>
public sealed record SharesRelatedFilter(
    string RelationshipKind,
    string With) : SelectionFilter;

// =============================================================================
// TAG FILTERS
// =============================================================================

/// <summary>Entity must have a specific tag with optional value match.</summary>
public sealed record HasTagFilter(
    string Tag,
    string Value) : SelectionFilter;

/// <summary>Entity must have ALL specified tags (AND semantics).</summary>
public sealed record HasTagsFilter(
    IReadOnlyList<string> Tags) : SelectionFilter;

/// <summary>Entity must have ANY of the specified tags (OR semantics).</summary>
public sealed record HasAnyTagFilter(
    IReadOnlyList<string> Tags) : SelectionFilter;

/// <summary>Entity must lack a specific tag (or the tag must not have the specified value).</summary>
public sealed record LacksTagFilter(
    string Tag,
    string Value) : SelectionFilter;

/// <summary>Entity must lack ALL of the specified tags.</summary>
public sealed record LacksAnyTagFilter(
    IReadOnlyList<string> Tags) : SelectionFilter;

// =============================================================================
// CULTURE FILTERS
// =============================================================================

/// <summary>Entity must have a specific culture.</summary>
public sealed record HasCultureFilter(
    string Culture) : SelectionFilter;

/// <summary>Entity must not have a specific culture.</summary>
public sealed record NotHasCultureFilter(
    string Culture) : SelectionFilter;

/// <summary>Entity's culture must match the culture of a referenced entity.</summary>
public sealed record MatchesCultureFilter(
    string With) : SelectionFilter;

/// <summary>Entity's culture must not match the culture of a referenced entity.</summary>
public sealed record NotMatchesCultureFilter(
    string With) : SelectionFilter;

// =============================================================================
// STATUS/PROMINENCE FILTERS
// =============================================================================

/// <summary>Entity must have a specific status.</summary>
public sealed record HasStatusFilter(
    string Status) : SelectionFilter;

/// <summary>Entity must have at least the specified prominence level.</summary>
public sealed record HasProminenceFilter(
    string MinProminence) : SelectionFilter;

// =============================================================================
// GRAPH PATH FILTER
// =============================================================================

/// <summary>Filter entities based on graph path traversal assertion.</summary>
public sealed record GraphPathFilter(
    GraphPathAssertion Assert) : SelectionFilter;

// =============================================================================
// COMPONENT SIZE FILTER
// =============================================================================

/// <summary>Filter entities based on connected component size.</summary>
public sealed record ComponentSizeFilter(
    IReadOnlyList<string> RelationshipKinds,
    int Min,
    int Max,
    double MinStrength) : SelectionFilter;
