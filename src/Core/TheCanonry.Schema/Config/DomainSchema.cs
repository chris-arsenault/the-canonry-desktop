namespace TheCanonry.Schema.Config;

public class DomainSchema
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required IReadOnlyList<EntityKindDefinition> EntityKinds { get; init; }
    public required IReadOnlyList<RelationshipKindDefinition> RelationshipKinds { get; init; }
    public required IReadOnlyList<CultureDefinition> Cultures { get; init; }
}
