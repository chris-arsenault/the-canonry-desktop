using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Schema.World;

public abstract record EntityEffect(string Description, string SemanticKind);

public sealed record CreatedEffect(string Description, string SemanticKind)
    : EntityEffect(Description, SemanticKind);

public sealed record EndedEffect(string Description, string SemanticKind)
    : EntityEffect(Description, SemanticKind);

public sealed record RelationshipFormedEffect(
    RelationshipKind RelationshipKind, NarrativeEntityRef RelatedEntity,
    string Description, string SemanticKind)
    : EntityEffect(Description, SemanticKind);

public sealed record RelationshipEndedEffect(
    RelationshipKind RelationshipKind, NarrativeEntityRef RelatedEntity,
    string Description, string SemanticKind)
    : EntityEffect(Description, SemanticKind);

public sealed record TagGainedEffect(string Tag, string Description, string SemanticKind)
    : EntityEffect(Description, SemanticKind);

public sealed record TagLostEffect(string Tag, string Description, string SemanticKind)
    : EntityEffect(Description, SemanticKind);

public sealed record FieldChangedEffect(
    string Field, object? PreviousValue, object? NewValue,
    string Description, string SemanticKind)
    : EntityEffect(Description, SemanticKind);

public sealed record ParticipantEffect(
    NarrativeEntityRef Entity, IReadOnlyList<EntityEffect> Effects);

public sealed record NarrativeEntityRef(
    EntityId Id, string Name, EntityKind Kind, string Subtype);
