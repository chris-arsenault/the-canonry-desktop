namespace TheCanonry.Engine.Tests.Graph;

using TheCanonry.Engine.Graph;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

public class EntityArchivalTests
{
    private static Entity CreateTestEntity(string id, string kind = "faction")
    {
        return new Entity(
            id: new EntityId(id),
            kind: new EntityKind(kind),
            subtype: "merchant",
            name: $"Test {id}",
            culture: new CultureId("northern"),
            eraId: new EraId("era-1"),
            coordinates: new SemanticCoordinates(0.5, 0.3, 0.1),
            createdBy: new ExecutionContext(1, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: 1);
    }

    private static Relationship CreateTestRelationship(
        string srcId, string dstId, string kind = "allied_with")
    {
        return new Relationship(
            sourceId: new EntityId(srcId),
            targetId: new EntityId(dstId),
            kind: new RelationshipKind(kind),
            strength: 0.5, distance: 0.0, category: "",
            createdBy: new ExecutionContext(1, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: 1);
    }

    [Fact]
    public void ArchiveEntity_SetsStatusToHistorical()
    {
        var graph = new GraphStore { Tick = 10 };
        graph.CreateEntity(CreateTestEntity("f1"));

        EntityArchival.ArchiveEntity(graph, new EntityId("f1"));

        var entity = graph.GetEntity(new EntityId("f1"));
        Assert.NotNull(entity);
        Assert.Equal(FrameworkPrimitives.Statuses.Historical, entity.Status);
    }

    [Fact]
    public void ArchiveEntity_ArchivesRelationships_ByDefault()
    {
        var graph = new GraphStore { Tick = 10 };
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));
        graph.AddRelationship(CreateTestRelationship("f1", "f2"));

        EntityArchival.ArchiveEntity(graph, new EntityId("f1"));

        // Active relationships should be empty
        Assert.Empty(graph.GetRelationships());
        // Historical relationships should exist
        Assert.Single(graph.GetRelationships(includeHistorical: true));
    }

    [Fact]
    public void ArchiveEntity_SkipsRelationships_WhenFalse()
    {
        var graph = new GraphStore { Tick = 10 };
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));
        graph.AddRelationship(CreateTestRelationship("f1", "f2"));

        EntityArchival.ArchiveEntity(graph, new EntityId("f1"), archiveRelationships: false);

        // Relationship should still be active
        Assert.Single(graph.GetRelationships());
    }

    [Fact]
    public void ArchiveEntities_ArchivesMultiple()
    {
        var graph = new GraphStore { Tick = 10 };
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));
        graph.CreateEntity(CreateTestEntity("f3"));

        EntityArchival.ArchiveEntities(graph,
            [new EntityId("f1"), new EntityId("f2")]);

        Assert.Equal(1, graph.GetEntityCount());
        Assert.Equal(3, graph.GetEntityCount(includeHistorical: true));
    }

    [Fact]
    public void ArchiveEntity_NoOp_WhenEntityNotFound()
    {
        var graph = new GraphStore { Tick = 10 };
        // Should not throw
        EntityArchival.ArchiveEntity(graph, new EntityId("missing"));
    }

    [Fact]
    public void TransferRelationships_MovesToTarget()
    {
        var graph = new GraphStore { Tick = 10 };
        graph.CreateEntity(CreateTestEntity("old"));
        graph.CreateEntity(CreateTestEntity("new"));
        graph.CreateEntity(CreateTestEntity("ally"));

        graph.AddRelationship(CreateTestRelationship("old", "ally", "allied_with"));

        var transferred = EntityArchival.TransferRelationships(
            graph, [new EntityId("old")], new EntityId("new"));

        Assert.Equal(1, transferred);

        // Old relationship should be archived
        Assert.False(graph.HasRelationship(new EntityId("old"), new EntityId("ally")));

        // New relationship should exist
        Assert.True(graph.HasRelationship(
            new EntityId("new"), new EntityId("ally"),
            new RelationshipKind("allied_with")));
    }

    [Fact]
    public void TransferRelationships_AvoidsSelfReferential()
    {
        var graph = new GraphStore { Tick = 10 };
        graph.CreateEntity(CreateTestEntity("old1"));
        graph.CreateEntity(CreateTestEntity("old2"));
        graph.CreateEntity(CreateTestEntity("new"));

        // Relationship between two entities that will both be transferred to 'new'
        graph.AddRelationship(CreateTestRelationship("old1", "old2", "allied_with"));

        var transferred = EntityArchival.TransferRelationships(
            graph, [new EntityId("old1"), new EntityId("old2")], new EntityId("new"));

        // Should not create self-referential relationship
        Assert.Equal(0, transferred);
        Assert.False(graph.HasRelationship(new EntityId("new"), new EntityId("new")));
    }

    [Fact]
    public void CreatePartOfRelationships_CreatesLinks()
    {
        var graph = new GraphStore { Tick = 10 };
        graph.CreateEntity(CreateTestEntity("member1"));
        graph.CreateEntity(CreateTestEntity("member2"));
        graph.CreateEntity(CreateTestEntity("container"));

        var created = EntityArchival.CreatePartOfRelationships(
            graph,
            [new EntityId("member1"), new EntityId("member2")],
            new EntityId("container"));

        Assert.Equal(2, created);
        Assert.True(graph.HasRelationship(
            new EntityId("member1"), new EntityId("container"),
            FrameworkPrimitives.RelationshipKinds.PartOf));
        Assert.True(graph.HasRelationship(
            new EntityId("member2"), new EntityId("container"),
            FrameworkPrimitives.RelationshipKinds.PartOf));
    }

    [Fact]
    public void CreatePartOfRelationships_SkipsDuplicates()
    {
        var graph = new GraphStore { Tick = 10 };
        graph.CreateEntity(CreateTestEntity("member1"));
        graph.CreateEntity(CreateTestEntity("container"));

        // Create one first
        EntityArchival.CreatePartOfRelationships(
            graph, [new EntityId("member1")], new EntityId("container"));

        // Try to create again
        var created = EntityArchival.CreatePartOfRelationships(
            graph, [new EntityId("member1")], new EntityId("container"));

        Assert.Equal(0, created);
    }
}
