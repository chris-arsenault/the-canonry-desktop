namespace TheCanonry.Engine.Rules.Types;

using TheCanonry.Engine.Graph;

/// <summary>
/// Abstract base for all mutation types. Mutations modify graph state.
/// </summary>
public abstract record Mutation;

// =============================================================================
// TAG MUTATIONS
// =============================================================================

/// <summary>Set a tag on an entity.</summary>
public sealed record SetTagMutation(
    string Entity,
    string Tag,
    string Value,
    string ValueFrom) : Mutation;

/// <summary>Remove a tag from an entity.</summary>
public sealed record RemoveTagMutation(
    string Entity,
    string Tag) : Mutation;

// =============================================================================
// RELATIONSHIP MUTATIONS
// =============================================================================

/// <summary>Create a relationship between two entities.</summary>
public sealed record CreateRelationshipMutation(
    string Src,
    string Dst,
    string Kind,
    double Strength,
    bool Bidirectional,
    string Category) : Mutation;

/// <summary>Archive a relationship (mark as historical).</summary>
public sealed record ArchiveRelationshipMutation(
    string Entity,
    string RelationshipKind,
    string With,
    Direction Direction) : Mutation;

/// <summary>Archive all relationships of a kind involving an entity.</summary>
public sealed record ArchiveAllRelationshipsMutation(
    string Entity,
    string RelationshipKind,
    Direction Direction) : Mutation;

/// <summary>Adjust relationship strength by a delta.</summary>
public sealed record AdjustRelationshipStrengthMutation(
    string Src,
    string Dst,
    string Kind,
    double Delta,
    bool Bidirectional) : Mutation;

/// <summary>Transfer a relationship from one entity to another.</summary>
public sealed record TransferRelationshipMutation(
    string Entity,
    string RelationshipKind,
    string From,
    string To,
    Condition Condition) : Mutation;

// =============================================================================
// ENTITY MUTATIONS
// =============================================================================

/// <summary>Change an entity's status.</summary>
public sealed record ChangeStatusMutation(
    string Entity,
    string NewStatus) : Mutation;

/// <summary>Adjust an entity's prominence by a delta.</summary>
public sealed record AdjustProminenceMutation(
    string Entity,
    double Delta) : Mutation;

// =============================================================================
// PRESSURE MUTATIONS
// =============================================================================

/// <summary>Modify a pressure value by a delta.</summary>
public sealed record ModifyPressureMutation(
    string PressureId,
    double Delta) : Mutation;

// =============================================================================
// RATE LIMIT MUTATIONS
// =============================================================================

/// <summary>Update rate limit state (marks creation tick and increments epoch count).</summary>
public sealed record UpdateRateLimitMutation() : Mutation;

// =============================================================================
// COMPOUND ACTIONS
// =============================================================================

/// <summary>Iterate over related entities and execute actions for each.</summary>
public sealed record ForEachRelatedAction(
    string Relationship,
    Direction Direction,
    string TargetKind,
    string TargetSubtype,
    IReadOnlyList<Mutation> Actions) : Mutation;

/// <summary>Conditionally execute actions based on a condition.</summary>
public sealed record ConditionalAction(
    Condition Condition,
    IReadOnlyList<Mutation> ThenActions,
    IReadOnlyList<Mutation> ElseActions) : Mutation;
