namespace TheCanonry.Engine.Rules.Types;

// =============================================================================
// GRAPH PATH ASSERTION
// =============================================================================

/// <summary>Type of graph path check.</summary>
public enum PathCheck
{
    Exists,
    NotExists,
    CountMin,
    CountMax
}

/// <summary>
/// Graph path assertion with steps, count, and constraints.
/// Used by both GraphPathCondition and GraphPathFilter.
/// </summary>
public sealed record GraphPathAssertion(
    PathCheck Check,
    IReadOnlyList<PathStep> Path,
    int Count,
    IReadOnlyList<PathConstraint> Where);

// =============================================================================
// PATH STEP
// =============================================================================

/// <summary>Direction for path traversal (uses out/in/any JSON convention).</summary>
public enum PathDirection
{
    Out,
    In,
    Any
}

/// <summary>A single step in a graph traversal path.</summary>
public sealed record PathStep(
    IReadOnlyList<string> Via,
    PathDirection Direction,
    string TargetKind,
    string TargetSubtype,
    string TargetStatus,
    IReadOnlyList<SelectionFilter> Filters,
    string As);

// =============================================================================
// PATH CONSTRAINT
// =============================================================================

/// <summary>
/// Abstract base for constraints on path target entities.
/// </summary>
public abstract record PathConstraint;

/// <summary>Target must not be in a stored set.</summary>
public sealed record NotInConstraint(string Set) : PathConstraint;

/// <summary>Target must be in a stored set.</summary>
public sealed record InConstraint(string Set) : PathConstraint;

/// <summary>Target must lack a specific relationship.</summary>
public sealed record LacksRelationshipConstraint(
    string Kind,
    string With,
    PathDirection Direction) : PathConstraint;

/// <summary>Target must have a specific relationship.</summary>
public sealed record HasRelationshipConstraint(
    string Kind,
    string With,
    PathDirection Direction) : PathConstraint;

/// <summary>Target must not be the starting entity.</summary>
public sealed record NotSelfConstraint() : PathConstraint;

/// <summary>Target must be of a specific entity kind.</summary>
public sealed record KindEqualsConstraint(string Kind) : PathConstraint;

/// <summary>Target must be of a specific subtype.</summary>
public sealed record SubtypeEqualsConstraint(string Subtype) : PathConstraint;
