using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;
using ExecutionContext = TheCanonry.Schema.World.ExecutionContext;

namespace TheCanonry.Engine.Tests.Rules;

/// <summary>
/// Shared helper methods for Rules test classes.
/// </summary>
internal static class TestHelpers
{
    internal static Entity CreateTestEntity(
        string id,
        string kind = "faction",
        string subtype = "merchant",
        string culture = "northern",
        string? status = null,
        double prominence = 2.0,
        int tick = 1)
    {
        var entity = new Entity(
            id: new EntityId(id),
            kind: new EntityKind(kind),
            subtype: subtype,
            name: $"Test {id}",
            culture: new CultureId(culture),
            eraId: new EraId("era-1"),
            coordinates: new SemanticCoordinates(0.5, 0.3, 0.1),
            createdBy: new ExecutionContext(tick, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: tick);

        if (status is not null)
            entity.UpdateStatus(new EntityStatus(status), tick);

        if (Math.Abs(prominence - 0.0) > 1e-9)
            entity.SetProminence(new Prominence(prominence), tick);

        return entity;
    }

    internal static Relationship CreateTestRelationship(
        string srcId,
        string dstId,
        string kind = "allied_with",
        double strength = 0.5,
        int tick = 1)
    {
        return new Relationship(
            sourceId: new EntityId(srcId),
            targetId: new EntityId(dstId),
            kind: new RelationshipKind(kind),
            strength: strength,
            distance: 0.0,
            category: "",
            createdBy: new ExecutionContext(tick, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: tick);
    }

    internal static GraphStore CreateGraphWithEntities(params Entity[] entities)
    {
        var graph = new GraphStore();
        foreach (var entity in entities)
            graph.CreateEntity(entity);
        return graph;
    }

    internal static RuleContext CreateSystemContext(GraphStore graph, Entity? self = null, string currentEraId = "era-1")
    {
        var ctx = RuleContext.CreateForSystem(graph, currentEraId);
        return self is not null ? ctx.WithSelf(self) : ctx;
    }
}
