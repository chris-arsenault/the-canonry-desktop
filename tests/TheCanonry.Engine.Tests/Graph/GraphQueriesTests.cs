using TheCanonry.Engine.Graph;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;
using ExecutionContext = TheCanonry.Schema.World.ExecutionContext;

namespace TheCanonry.Engine.Tests.Graph;

public class GraphQueriesTests
{
    private static Entity CreateTestEntity(
        string id,
        string kind = "faction",
        string subtype = "merchant",
        string culture = "northern")
    {
        return new Entity(
            id: new EntityId(id),
            kind: new EntityKind(kind),
            subtype: subtype,
            name: $"Test {id}",
            culture: new CultureId(culture),
            eraId: new EraId("era-1"),
            coordinates: new SemanticCoordinates(0.5, 0.3, 0.1),
            createdBy: new ExecutionContext(1, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: 1);
    }

    private static Relationship CreateTestRelationship(
        string srcId,
        string dstId,
        string kind = "allied_with",
        double strength = 0.5)
    {
        return new Relationship(
            sourceId: new EntityId(srcId),
            targetId: new EntityId(dstId),
            kind: new RelationshipKind(kind),
            strength: strength,
            distance: 0.0,
            category: "",
            createdBy: new ExecutionContext(1, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: 1);
    }

    private static GraphStore SetupGraphWithRelationships()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));
        graph.CreateEntity(CreateTestEntity("f3"));
        graph.CreateEntity(CreateTestEntity("f4"));

        graph.AddRelationship(CreateTestRelationship("f1", "f2", "allied_with"));
        graph.AddRelationship(CreateTestRelationship("f1", "f3", "allied_with"));
        graph.AddRelationship(CreateTestRelationship("f4", "f1", "rival_of"));

        return graph;
    }

    // =========================================================================
    // CountRelationships
    // =========================================================================

    [Fact]
    public void CountRelationships_AsSource()
    {
        var graph = SetupGraphWithRelationships();
        var count = GraphQueries.CountRelationships(
            graph, new EntityId("f1"), new RelationshipKind("allied_with"), Direction.Source);

        Assert.Equal(2, count);
    }

    [Fact]
    public void CountRelationships_AsTarget()
    {
        var graph = SetupGraphWithRelationships();
        var count = GraphQueries.CountRelationships(
            graph, new EntityId("f1"), new RelationshipKind("rival_of"), Direction.Target);

        Assert.Equal(1, count);
    }

    [Fact]
    public void CountRelationships_ReturnsZero_WhenNoMatch()
    {
        var graph = SetupGraphWithRelationships();
        var count = GraphQueries.CountRelationships(
            graph, new EntityId("f1"), new RelationshipKind("trade_with"), Direction.Both);

        Assert.Equal(0, count);
    }

    // =========================================================================
    // GetEntitiesByRelationship
    // =========================================================================

    [Fact]
    public void GetEntitiesByRelationship_ReturnsConnected()
    {
        var graph = SetupGraphWithRelationships();
        var allies = GraphQueries.GetEntitiesByRelationship(
            graph, new EntityId("f1"), new RelationshipKind("allied_with"), Direction.Source);

        Assert.Equal(2, allies.Count);
    }

    // =========================================================================
    // FindRelationship
    // =========================================================================

    [Fact]
    public void FindRelationship_ReturnsFirst()
    {
        var graph = SetupGraphWithRelationships();
        var rel = GraphQueries.FindRelationship(
            graph, new EntityId("f1"), new RelationshipKind("allied_with"), Direction.Source);

        Assert.NotNull(rel);
        Assert.Equal(new EntityId("f1"), rel.SourceId);
    }

    [Fact]
    public void FindRelationship_ReturnsNull_WhenNoMatch()
    {
        var graph = SetupGraphWithRelationships();
        var rel = GraphQueries.FindRelationship(
            graph, new EntityId("f1"), new RelationshipKind("trade_with"), Direction.Source);

        Assert.Null(rel);
    }

    // =========================================================================
    // GetRelatedEntity
    // =========================================================================

    [Fact]
    public void GetRelatedEntity_ReturnsOtherEnd()
    {
        var graph = SetupGraphWithRelationships();
        var rel = GraphQueries.FindRelationship(
            graph, new EntityId("f1"), new RelationshipKind("allied_with"), Direction.Source);

        var related = GraphQueries.GetRelatedEntity(graph, rel, new EntityId("f1"));

        Assert.NotNull(related);
        // Should be f2 or f3, the other end of f1's allied_with
        Assert.NotEqual(new EntityId("f1"), related.Id);
    }

    [Fact]
    public void GetRelatedEntity_ReturnsNull_WhenRelationshipIsNull()
    {
        var graph = SetupGraphWithRelationships();
        var related = GraphQueries.GetRelatedEntity(graph, null, new EntityId("f1"));

        Assert.Null(related);
    }
}
