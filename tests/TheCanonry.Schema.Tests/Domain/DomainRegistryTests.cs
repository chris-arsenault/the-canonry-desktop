using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Primitives;

namespace TheCanonry.Schema.Tests.Domain;

public class DomainRegistryTests
{
    private DomainSchema CreateTestSchema()
    {
        return new DomainSchema
        {
            Id = "test",
            Name = "Test",
            Version = "1.0",
            EntityKinds =
            [
                new EntityKindDefinition
                {
                    Kind = new EntityKind("faction"),
                    Description = "A faction",
                    IsFramework = false,
                    Category = EntityCategory.Collective,
                    Subtypes = [],
                    Statuses =
                    [
                        new StatusDefinition { Id = "active", Name = "Active" },
                        new StatusDefinition { Id = "dissolved", Name = "Dissolved", IsTerminal = true, Polarity = Polarity.Negative, TransitionVerb = "dissolved" },
                    ],
                    DefaultStatus = new EntityStatus("active"),
                }
            ],
            RelationshipKinds =
            [
                new RelationshipKindDefinition
                {
                    Kind = new RelationshipKind("rivalry"),
                    Name = "Rivalry",
                    Description = "A rivalry",
                    IsFramework = false,
                }
            ],
            Cultures = [],
        };
    }

    [Fact]
    public void Registry_includes_framework_entity_kinds()
    {
        var registry = new DomainRegistry(CreateTestSchema());
        registry.ValidateEntityKind(FrameworkPrimitives.EntityKinds.Era); // Should not throw
    }

    [Fact]
    public void Registry_includes_domain_entity_kinds()
    {
        var registry = new DomainRegistry(CreateTestSchema());
        registry.ValidateEntityKind(new EntityKind("faction")); // Should not throw
    }

    [Fact]
    public void Registry_rejects_unknown_entity_kinds()
    {
        var registry = new DomainRegistry(CreateTestSchema());
        Assert.Throws<InvalidDomainValueException>(() =>
            registry.ValidateEntityKind(new EntityKind("spaceship")));
    }

    [Fact]
    public void Registry_validates_status_for_entity_kind()
    {
        var registry = new DomainRegistry(CreateTestSchema());
        registry.ValidateStatus(new EntityKind("faction"), new EntityStatus("dissolved")); // Should not throw
    }

    [Fact]
    public void Registry_rejects_invalid_status_for_entity_kind()
    {
        var registry = new DomainRegistry(CreateTestSchema());
        Assert.Throws<InvalidDomainValueException>(() =>
            registry.ValidateStatus(new EntityKind("faction"), new EntityStatus("exploded")));
    }

    [Fact]
    public void Registry_includes_domain_relationship_kinds()
    {
        var registry = new DomainRegistry(CreateTestSchema());
        registry.ValidateRelationshipKind(new RelationshipKind("rivalry")); // Should not throw
    }

    [Fact]
    public void Registry_includes_framework_relationship_kinds()
    {
        var registry = new DomainRegistry(CreateTestSchema());
        registry.ValidateRelationshipKind(FrameworkPrimitives.RelationshipKinds.Supersedes); // Should not throw
    }
}
