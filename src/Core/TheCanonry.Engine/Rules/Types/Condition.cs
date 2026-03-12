namespace TheCanonry.Engine.Rules.Types;

using TheCanonry.Engine.Graph;

/// <summary>
/// Abstract base for all condition types. Conditions evaluate to boolean.
/// Sealed record subtypes enable exhaustive pattern matching.
/// </summary>
public abstract record Condition;

// =============================================================================
// PRESSURE CONDITIONS
// =============================================================================

/// <summary>Check if a pressure value falls within [Min, Max].</summary>
public sealed record PressureCondition(
    string PressureId,
    double Min,
    double Max) : Condition;

/// <summary>Compare two pressure values using an operator.</summary>
public sealed record PressureCompareCondition(
    string PressureA,
    string PressureB,
    ComparisonOperator Operator) : Condition;

/// <summary>Check if any of the given pressures exceeds a threshold.</summary>
public sealed record PressureAnyAboveCondition(
    IReadOnlyList<string> PressureIds,
    double Threshold) : Condition;

// =============================================================================
// ENTITY COUNT CONDITIONS
// =============================================================================

/// <summary>Check entity count against min/max bounds with overshoot factor.</summary>
public sealed record EntityCountCondition(
    string Kind,
    string Subtype,
    string Status,
    int Min,
    int Max,
    double OvershootFactor) : Condition;

// =============================================================================
// RELATIONSHIP CONDITIONS
// =============================================================================

/// <summary>Check relationship count for the current entity.</summary>
public sealed record RelationshipCountCondition(
    string RelationshipKind,
    Direction Direction,
    int Min,
    int Max) : Condition;

/// <summary>Check if a specific relationship exists from the current entity.</summary>
public sealed record RelationshipExistsCondition(
    string RelationshipKind,
    string With,
    Direction Direction,
    string TargetKind,
    string TargetSubtype,
    string TargetStatus) : Condition;

// =============================================================================
// TAG CONDITIONS
// =============================================================================

/// <summary>Check if a tag exists on an entity with an optional value match.</summary>
public sealed record TagExistsCondition(
    string Entity,
    string Tag,
    string Value) : Condition;

/// <summary>Check if a tag is absent from an entity.</summary>
public sealed record LacksTagCondition(
    string Entity,
    string Tag) : Condition;

// =============================================================================
// STATUS/PROMINENCE CONDITIONS
// =============================================================================

/// <summary>Check entity status, optionally negated.</summary>
public sealed record StatusCondition(
    string Status,
    bool Not) : Condition;

/// <summary>Check entity prominence is within a label range.</summary>
public sealed record ProminenceCondition(
    string MinLabel,
    string MaxLabel) : Condition;

// =============================================================================
// TIME CONDITIONS
// =============================================================================

/// <summary>Check time elapsed since entity creation or last update.</summary>
public sealed record TimeElapsedCondition(
    int MinTicks,
    string Since) : Condition;

/// <summary>Check if cooldown has elapsed since last template creation.</summary>
public sealed record CooldownElapsedCondition(
    int CooldownTicks) : Condition;

/// <summary>Check creations per epoch limit.</summary>
public sealed record CreationsPerEpochCondition(
    int MaxPerEpoch) : Condition;

/// <summary>Check completed growth phases in an era.</summary>
public sealed record GrowthPhasesCompleteCondition(
    int MinPhases,
    string EraId) : Condition;

// =============================================================================
// ERA CONDITIONS
// =============================================================================

/// <summary>Check if the current era matches one of the allowed era IDs.</summary>
public sealed record EraMatchCondition(
    IReadOnlyList<string> Eras) : Condition;

// =============================================================================
// PROBABILITY CONDITIONS
// =============================================================================

/// <summary>Random chance condition (0-1 probability).</summary>
public sealed record RandomChanceCondition(
    double Chance) : Condition;

// =============================================================================
// GRAPH PATH CONDITIONS
// =============================================================================

/// <summary>Check a graph path assertion starting from the current entity.</summary>
public sealed record GraphPathCondition(
    GraphPathAssertion Assert) : Condition;

// =============================================================================
// GRAPH TOPOLOGY CONDITIONS
// =============================================================================

/// <summary>Check connected component size against min/max bounds.</summary>
public sealed record ComponentSizeCondition(
    IReadOnlyList<string> RelationshipKinds,
    int Min,
    int Max,
    double MinStrength) : Condition;

// =============================================================================
// ENTITY EXISTENCE CONDITIONS
// =============================================================================

/// <summary>Check if an entity reference resolves.</summary>
public sealed record EntityExistsCondition(
    string Entity) : Condition;

/// <summary>Check if an entity has a relationship of the given kind.</summary>
public sealed record EntityHasRelationshipCondition(
    string Entity,
    string RelationshipKind,
    Direction Direction) : Condition;

// =============================================================================
// COMPOSITE CONDITIONS
// =============================================================================

/// <summary>All child conditions must pass (AND logic).</summary>
public sealed record AndCondition(
    IReadOnlyList<Condition> Conditions) : Condition;

/// <summary>At least one child condition must pass (OR logic).</summary>
public sealed record OrCondition(
    IReadOnlyList<Condition> Conditions) : Condition;

/// <summary>Always passes.</summary>
public sealed record AlwaysCondition() : Condition;
