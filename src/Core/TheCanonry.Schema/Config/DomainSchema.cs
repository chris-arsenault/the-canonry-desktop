namespace TheCanonry.Schema.Config;

public class DomainSchema
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required IReadOnlyList<EntityKindDefinition> EntityKinds { get; init; }
    public required IReadOnlyList<RelationshipKindDefinition> RelationshipKinds { get; init; }
    public required IReadOnlyList<CultureDefinition> Cultures { get; init; }
    public IReadOnlyList<TagDefinition> TagRegistry { get; init; } = [];
    public IReadOnlyList<AxisDefinition> AxisDefinitions { get; init; } = [];
    public IReadOnlyList<SeedEntity> SeedEntities { get; init; } = [];
    public IReadOnlyList<SeedRelationship> SeedRelationships { get; init; } = [];
}
