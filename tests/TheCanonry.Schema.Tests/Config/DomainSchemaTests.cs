using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;

namespace TheCanonry.Schema.Tests.Config;

public class DomainSchemaTests
{
    [Fact]
    public void EntityKindDefinition_has_required_properties()
    {
        var def = new EntityKindDefinition
        {
            Kind = new EntityKind("faction"),
            Description = "A political faction",
            IsFramework = false,
            Category = EntityCategory.Collective,
            Subtypes = [new SubtypeDefinition { Id = "merchant", Name = "Merchant Guild" }],
            Statuses = [new StatusDefinition { Id = "active", Name = "Active", IsTerminal = false, Polarity = Polarity.Neutral, TransitionVerb = "became" }],
            DefaultStatus = new EntityStatus("active"),
        };

        Assert.Equal(new EntityKind("faction"), def.Kind);
        Assert.Single(def.Subtypes);
    }

    [Fact]
    public void DomainSchema_has_entity_and_relationship_kinds()
    {
        var schema = new DomainSchema
        {
            Id = "test",
            Name = "Test Domain",
            Version = "1.0",
            EntityKinds = [],
            RelationshipKinds = [],
            Cultures = [],
        };

        Assert.Equal("test", schema.Id);
    }
}
