using TheCanonry.Engine.Graph;

namespace TheCanonry.Engine.Rules.Types;

/// <summary>
/// Abstract base for all metric types. Metrics evaluate to a double.
/// </summary>
public abstract record Metric;

// =============================================================================
// COUNT METRICS
// =============================================================================

/// <summary>Count entities matching kind/subtype/status criteria.</summary>
public sealed record EntityCountMetric(
    string Kind,
    string Subtype,
    string Status,
    double Coefficient,
    double Cap) : Metric;

/// <summary>Count relationships matching criteria for the current entity.</summary>
public sealed record RelationshipCountMetric(
    IReadOnlyList<string> RelationshipKinds,
    Direction Direction,
    double MinStrength,
    double Coefficient,
    double Cap) : Metric;

/// <summary>Count entities with specific tags.</summary>
public sealed record TagCountMetric(
    IReadOnlyList<string> Tags,
    double Coefficient,
    double Cap) : Metric;

/// <summary>Total entity count in the graph.</summary>
public sealed record TotalEntitiesMetric(
    double Coefficient,
    double Cap) : Metric;

/// <summary>Constant value metric.</summary>
public sealed record ConstantMetric(
    double Value,
    double Coefficient) : Metric;

/// <summary>Count all connections (relationships) involving an entity.</summary>
public sealed record ConnectionCountMetric(
    IReadOnlyList<string> RelationshipKinds,
    Direction Direction,
    double MinStrength,
    double Coefficient,
    double Cap) : Metric;

// =============================================================================
// RATIO METRICS
// =============================================================================

/// <summary>Simple count metric used as numerator/denominator in RatioMetric.</summary>
public abstract record SimpleCountMetric;

public sealed record SimpleEntityCount(string Kind, string Subtype, string Status) : SimpleCountMetric;
public sealed record SimpleRelationshipCount(IReadOnlyList<string> RelationshipKinds) : SimpleCountMetric;
public sealed record SimpleTagCount(IReadOnlyList<string> Tags) : SimpleCountMetric;
public sealed record SimpleTotalEntities() : SimpleCountMetric;
public sealed record SimpleConstant(double Value) : SimpleCountMetric;

/// <summary>Calculate ratio of two simple counts.</summary>
public sealed record RatioMetric(
    SimpleCountMetric Numerator,
    SimpleCountMetric Denominator,
    double FallbackValue,
    double Coefficient,
    double Cap) : Metric;

/// <summary>Ratio of entities with alive vs dead status.</summary>
public sealed record StatusRatioMetric(
    string Kind,
    string Subtype,
    string AliveStatus,
    double Coefficient,
    double Cap) : Metric;

/// <summary>Ratio of cross-culture relationships to total relationships.</summary>
public sealed record CrossCultureRatioMetric(
    IReadOnlyList<string> RelationshipKinds,
    double Coefficient,
    double Cap) : Metric;

// =============================================================================
// EVOLUTION METRICS
// =============================================================================

/// <summary>Count entities sharing a specific relationship (common enemies, trade partners, etc.).</summary>
public sealed record SharedRelationshipMetric(
    IReadOnlyList<string> SharedRelationshipKinds,
    Direction SharedDirection,
    SharedRelationshipVia? Via,
    double MinStrength,
    double Coefficient,
    double Cap) : Metric;

/// <summary>Configuration for the optional intermediate hop in SharedRelationshipMetric.</summary>
public sealed record SharedRelationshipVia(
    string RelationshipKind,
    Direction Direction,
    string IntermediateKind);

// =============================================================================
// PROMINENCE METRICS
// =============================================================================

/// <summary>Returns a multiplier based on entity prominence level.</summary>
public sealed record ProminenceMultiplierMetric(
    string Mode) : Metric;

/// <summary>Average prominence of connected entities.</summary>
public sealed record NeighborProminenceMetric(
    IReadOnlyList<string> RelationshipKinds,
    Direction Direction,
    double MinStrength,
    double Coefficient,
    double Cap) : Metric;

// =============================================================================
// NEIGHBOR METRICS
// =============================================================================

/// <summary>Count neighboring entities of a specific kind via relationship chain.</summary>
public sealed record NeighborKindCountMetric(
    IReadOnlyList<string> Via,
    Direction ViaDirection,
    string Then,
    Direction ThenDirection,
    string Kind,
    string Subtype,
    string Status,
    string HasTag,
    double MinStrength,
    double Coefficient,
    double Cap) : Metric;

// =============================================================================
// GRAPH TOPOLOGY METRICS
// =============================================================================

/// <summary>Size of the connected component containing an entity.</summary>
public sealed record ComponentSizeMetric(
    IReadOnlyList<string> RelationshipKinds,
    double MinStrength,
    double Coefficient,
    double Cap) : Metric;

// =============================================================================
// DECAY/FALLOFF METRICS
// =============================================================================

/// <summary>Decay rate value (none/slow/medium/fast).</summary>
public sealed record DecayRateMetric(
    string Rate) : Metric;

/// <summary>Distance falloff calculation.</summary>
public sealed record FalloffMetric(
    string FalloffType,
    double Distance,
    double MaxDistance) : Metric;
