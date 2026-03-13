using TheCanonry.Engine.Graph;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;
using ExecutionContext = TheCanonry.Schema.World.ExecutionContext;

namespace TheCanonry.Engine.Tests.Graph;

public class GraphStoreTests
{
    private static Entity CreateTestEntity(
        string id,
        string kind = "faction",
        string subtype = "merchant",
        string culture = "northern",
        EntityStatus? status = null)
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

    // =========================================================================
    // ENTITY CREATION & RETRIEVAL
    // =========================================================================

    [Fact]
    public void CreateEntity_StoresAndRetrievesById()
    {
        var graph = new GraphStore();
        var entity = CreateTestEntity("faction-1");

        graph.CreateEntity(entity);
        var retrieved = graph.GetEntity(new EntityId("faction-1"));

        Assert.NotNull(retrieved);
        Assert.Equal(new EntityId("faction-1"), retrieved.Id);
        Assert.Equal("Test faction-1", retrieved.Name);
    }

    [Fact]
    public void CreateEntity_DuplicateId_Throws()
    {
        var graph = new GraphStore();
        var entity1 = CreateTestEntity("faction-1");
        var entity2 = CreateTestEntity("faction-1");

        graph.CreateEntity(entity1);
        Assert.Throws<ArgumentException>(() => graph.CreateEntity(entity2));
    }

    [Fact]
    public void HasEntity_ReturnsCorrectly()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("faction-1"));

        Assert.True(graph.HasEntity(new EntityId("faction-1")));
        Assert.False(graph.HasEntity(new EntityId("faction-2")));
    }

    // =========================================================================
    // ENTITY COUNT
    // =========================================================================

    [Fact]
    public void GetEntityCount_ExcludesHistorical_ByDefault()
    {
        var graph = new GraphStore { Tick = 5 };
        var active = CreateTestEntity("active-1");
        var historical = CreateTestEntity("historical-1");

        graph.CreateEntity(active);
        graph.CreateEntity(historical);
        historical.UpdateStatus(FrameworkPrimitives.Statuses.Historical, 5);

        Assert.Equal(1, graph.GetEntityCount());
    }

    [Fact]
    public void GetEntityCount_IncludesHistorical_WhenRequested()
    {
        var graph = new GraphStore { Tick = 5 };
        var active = CreateTestEntity("active-1");
        var historical = CreateTestEntity("historical-1");

        graph.CreateEntity(active);
        graph.CreateEntity(historical);
        historical.UpdateStatus(FrameworkPrimitives.Statuses.Historical, 5);

        Assert.Equal(2, graph.GetEntityCount(includeHistorical: true));
    }

    // =========================================================================
    // GET ENTITIES
    // =========================================================================

    [Fact]
    public void GetEntities_ExcludesHistorical_ByDefault()
    {
        var graph = new GraphStore { Tick = 5 };
        var active = CreateTestEntity("active-1");
        var historical = CreateTestEntity("historical-1");

        graph.CreateEntity(active);
        graph.CreateEntity(historical);
        historical.UpdateStatus(FrameworkPrimitives.Statuses.Historical, 5);

        var entities = graph.GetEntities();
        Assert.Single(entities);
        Assert.Equal(new EntityId("active-1"), entities[0].Id);
    }

    // =========================================================================
    // FIND ENTITIES
    // =========================================================================

    [Fact]
    public void FindEntities_ByKind()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1", kind: "faction"));
        graph.CreateEntity(CreateTestEntity("n1", kind: "npc"));
        graph.CreateEntity(CreateTestEntity("f2", kind: "faction"));

        var factions = graph.FindEntities(EntityCriteria.Create(kind: new EntityKind("faction")));

        Assert.Equal(2, factions.Count);
    }

    [Fact]
    public void FindEntities_BySubtype()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1", subtype: "merchant"));
        graph.CreateEntity(CreateTestEntity("f2", subtype: "military"));
        graph.CreateEntity(CreateTestEntity("f3", subtype: "merchant"));

        var merchants = graph.FindEntities(EntityCriteria.Create(subtype: "merchant"));

        Assert.Equal(2, merchants.Count);
    }

    [Fact]
    public void FindEntities_ByCulture()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1", culture: "northern"));
        graph.CreateEntity(CreateTestEntity("f2", culture: "southern"));
        graph.CreateEntity(CreateTestEntity("f3", culture: "northern"));

        var northern = graph.FindEntities(EntityCriteria.Create(culture: new CultureId("northern")));

        Assert.Equal(2, northern.Count);
    }

    [Fact]
    public void FindEntities_ByTag()
    {
        var graph = new GraphStore();
        var e1 = CreateTestEntity("f1");
        e1.Tags.Set("warlike", true);
        var e2 = CreateTestEntity("f2");
        var e3 = CreateTestEntity("f3");
        e3.Tags.Set("warlike", true);

        graph.CreateEntity(e1);
        graph.CreateEntity(e2);
        graph.CreateEntity(e3);

        var warlike = graph.FindEntities(EntityCriteria.Create(tag: "warlike"));

        Assert.Equal(2, warlike.Count);
    }

    [Fact]
    public void FindEntities_ByStatus()
    {
        var graph = new GraphStore { Tick = 5 };
        var active = CreateTestEntity("active-1");
        var historical = CreateTestEntity("historical-1");

        graph.CreateEntity(active);
        graph.CreateEntity(historical);
        historical.UpdateStatus(FrameworkPrimitives.Statuses.Historical, 5);

        // Search for historical entities specifically (must include historical)
        var results = graph.FindEntities(EntityCriteria.Create(
            status: FrameworkPrimitives.Statuses.Historical,
            includeHistorical: true));

        Assert.Single(results);
        Assert.Equal(new EntityId("historical-1"), results[0].Id);
    }

    [Fact]
    public void FindEntities_WithExcludeList()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));
        graph.CreateEntity(CreateTestEntity("f3"));

        var results = graph.FindEntities(EntityCriteria.Create(
            exclude: [new EntityId("f2")]));

        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(results, e => e.Id == new EntityId("f2"));
    }

    // =========================================================================
    // GET ENTITIES BY KIND
    // =========================================================================

    [Fact]
    public void GetEntitiesByKind_ReturnsMatching()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1", kind: "faction"));
        graph.CreateEntity(CreateTestEntity("n1", kind: "npc"));

        var factions = graph.GetEntitiesByKind(new EntityKind("faction"));

        Assert.Single(factions);
        Assert.Equal(new EntityKind("faction"), factions[0].Kind);
    }

    // =========================================================================
    // RELATIONSHIP OPERATIONS
    // =========================================================================

    [Fact]
    public void AddRelationship_And_GetRelationships()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));

        var rel = CreateTestRelationship("f1", "f2");
        var added = graph.AddRelationship(rel);

        Assert.True(added);
        var rels = graph.GetRelationships();
        Assert.Single(rels);
        Assert.Equal(new EntityId("f1"), rels[0].SourceId);
        Assert.Equal(new EntityId("f2"), rels[0].TargetId);
    }

    [Fact]
    public void AddRelationship_RejectsDuplicate()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));

        var rel1 = CreateTestRelationship("f1", "f2");
        var rel2 = CreateTestRelationship("f1", "f2");

        Assert.True(graph.AddRelationship(rel1));
        Assert.False(graph.AddRelationship(rel2));
    }

    [Fact]
    public void AddRelationship_RejectsWhenEntityMissing()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));

        var rel = CreateTestRelationship("f1", "missing");
        Assert.False(graph.AddRelationship(rel));
    }

    [Fact]
    public void FindRelationships_ByKind()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));
        graph.CreateEntity(CreateTestEntity("f3"));

        graph.AddRelationship(CreateTestRelationship("f1", "f2", "allied_with"));
        graph.AddRelationship(CreateTestRelationship("f1", "f3", "rival_of"));

        var allied = graph.FindRelationships(RelationshipCriteria.Create(
            kind: new RelationshipKind("allied_with")));

        Assert.Single(allied);
        Assert.Equal(new RelationshipKind("allied_with"), allied[0].Kind);
    }

    [Fact]
    public void GetEntityRelationships_ByDirection()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));
        graph.CreateEntity(CreateTestEntity("f3"));

        graph.AddRelationship(CreateTestRelationship("f1", "f2")); // f1 is source
        graph.AddRelationship(CreateTestRelationship("f3", "f1")); // f1 is target

        var asSource = graph.GetEntityRelationships(new EntityId("f1"), Direction.Source);
        Assert.Single(asSource);
        Assert.Equal(new EntityId("f2"), asSource[0].TargetId);

        var asTarget = graph.GetEntityRelationships(new EntityId("f1"), Direction.Target);
        Assert.Single(asTarget);
        Assert.Equal(new EntityId("f3"), asTarget[0].SourceId);

        var both = graph.GetEntityRelationships(new EntityId("f1"), Direction.Both);
        Assert.Equal(2, both.Count);
    }

    [Fact]
    public void HasRelationship_ChecksCorrectly()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));
        graph.AddRelationship(CreateTestRelationship("f1", "f2", "allied_with"));

        Assert.True(graph.HasRelationship(new EntityId("f1"), new EntityId("f2")));
        Assert.True(graph.HasRelationship(new EntityId("f1"), new EntityId("f2"), new RelationshipKind("allied_with")));
        Assert.False(graph.HasRelationship(new EntityId("f1"), new EntityId("f2"), new RelationshipKind("rival_of")));
        Assert.False(graph.HasRelationship(new EntityId("f2"), new EntityId("f1"))); // direction matters
    }

    [Fact]
    public void RemoveRelationship_Works()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));
        graph.AddRelationship(CreateTestRelationship("f1", "f2", "allied_with"));

        Assert.True(graph.RemoveRelationship(
            new EntityId("f1"), new EntityId("f2"), new RelationshipKind("allied_with")));
        Assert.Empty(graph.GetRelationships());

        // Removing non-existent returns false
        Assert.False(graph.RemoveRelationship(
            new EntityId("f1"), new EntityId("f2"), new RelationshipKind("allied_with")));
    }

    // =========================================================================
    // CONNECTED ENTITIES
    // =========================================================================

    [Fact]
    public void GetConnectedEntities_TraversesRelationships()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));
        graph.CreateEntity(CreateTestEntity("f3"));

        graph.AddRelationship(CreateTestRelationship("f1", "f2", "allied_with"));
        graph.AddRelationship(CreateTestRelationship("f3", "f1", "rival_of"));

        // f1 as source should find f2
        var fromSource = graph.GetConnectedEntities(
            new EntityId("f1"),
            direction: Direction.Source);
        Assert.Single(fromSource);
        Assert.Equal(new EntityId("f2"), fromSource[0].Id);

        // f1 as target should find f3
        var fromTarget = graph.GetConnectedEntities(
            new EntityId("f1"),
            direction: Direction.Target);
        Assert.Single(fromTarget);
        Assert.Equal(new EntityId("f3"), fromTarget[0].Id);

        // f1 both directions should find f2 and f3
        var fromBoth = graph.GetConnectedEntities(new EntityId("f1"));
        Assert.Equal(2, fromBoth.Count);
    }

    [Fact]
    public void GetConnectedEntities_FiltersByRelationKind()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));
        graph.CreateEntity(CreateTestEntity("f3"));

        graph.AddRelationship(CreateTestRelationship("f1", "f2", "allied_with"));
        graph.AddRelationship(CreateTestRelationship("f1", "f3", "rival_of"));

        var allies = graph.GetConnectedEntities(
            new EntityId("f1"),
            relationKind: new RelationshipKind("allied_with"));
        Assert.Single(allies);
        Assert.Equal(new EntityId("f2"), allies[0].Id);
    }

    [Fact]
    public void GetConnectedEntities_ExcludesHistorical_ByDefault()
    {
        var graph = new GraphStore { Tick = 5 };
        graph.CreateEntity(CreateTestEntity("f1"));
        var historical = CreateTestEntity("f2");
        graph.CreateEntity(historical);

        graph.AddRelationship(CreateTestRelationship("f1", "f2"));
        historical.UpdateStatus(FrameworkPrimitives.Statuses.Historical, 5);

        var connected = graph.GetConnectedEntities(new EntityId("f1"));
        Assert.Empty(connected);

        var connectedAll = graph.GetConnectedEntities(
            new EntityId("f1"), includeHistorical: true);
        Assert.Single(connectedAll);
    }

    // =========================================================================
    // UPDATE & DELETE
    // =========================================================================

    [Fact]
    public void UpdateEntity_MutatesViaCallback()
    {
        var graph = new GraphStore { Tick = 10 };
        graph.CreateEntity(CreateTestEntity("f1"));

        var updated = graph.UpdateEntity(new EntityId("f1"), e =>
            e.SetName("New Name", graph.Tick));

        Assert.True(updated);
        Assert.Equal("New Name", graph.GetEntity(new EntityId("f1"))!.Name);
    }

    [Fact]
    public void UpdateEntity_ReturnsFalse_WhenNotFound()
    {
        var graph = new GraphStore();
        Assert.False(graph.UpdateEntity(new EntityId("missing"), _ => { }));
    }

    [Fact]
    public void DeleteEntity_RemovesEntity()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));

        Assert.True(graph.DeleteEntity(new EntityId("f1")));
        Assert.False(graph.HasEntity(new EntityId("f1")));
        Assert.False(graph.DeleteEntity(new EntityId("f1"))); // second delete returns false
    }

    // =========================================================================
    // PRESSURES & TICK
    // =========================================================================

    [Fact]
    public void Tick_And_Pressures_AreAccessible()
    {
        var graph = new GraphStore { Tick = 42 };
        graph.Pressures["conflict"] = 0.75;
        graph.Pressures["prosperity"] = -0.3;

        Assert.Equal(42, graph.Tick);
        Assert.Equal(0.75, graph.Pressures["conflict"]);
        Assert.Equal(-0.3, graph.Pressures["prosperity"]);
    }

    // =========================================================================
    // HISTORICAL RELATIONSHIP FILTERING
    // =========================================================================

    [Fact]
    public void GetRelationships_ExcludesArchived_ByDefault()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));

        var rel = CreateTestRelationship("f1", "f2");
        graph.AddRelationship(rel);
        rel.Archive(5);

        Assert.Empty(graph.GetRelationships());
        Assert.Single(graph.GetRelationships(includeHistorical: true));
    }

    [Fact]
    public void HasRelationship_IgnoresArchived()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateTestEntity("f1"));
        graph.CreateEntity(CreateTestEntity("f2"));

        var rel = CreateTestRelationship("f1", "f2");
        graph.AddRelationship(rel);
        rel.Archive(5);

        Assert.False(graph.HasRelationship(new EntityId("f1"), new EntityId("f2")));
    }
}
