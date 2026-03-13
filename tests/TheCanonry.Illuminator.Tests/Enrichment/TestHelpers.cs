using TheCanonry.Illuminator.Enrichment;
using TheCanonry.Illuminator.Enrichment.Tasks;
using TheCanonry.Illuminator.Types;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

namespace TheCanonry.Illuminator.Tests.Enrichment;

internal static class TestHelpers
{
    public static DomainSchema MakeSchema() => new()
    {
        Id = "test",
        Name = "Test Domain",
        Version = "1.0",
        EntityKinds = [],
        RelationshipKinds = [],
        Cultures = [],
    };

    public static Entity MakeEntity(string id = "e1", string name = "Test Entity")
    {
        return new Entity(
            new EntityId(id),
            FrameworkPrimitives.EntityKinds.Era, // any valid kind
            "scholar",
            name,
            new CultureId("northern"),
            new EraId("era1"),
            new SemanticCoordinates(0.5, 0.5, 0.0),
            new Schema.World.ExecutionContext(1, ExecutionSource.Seed, "test", true, ""),
            tick: 1);
    }

    public static TaskContext MakeContext(MockLlmClient llmClient) => new()
    {
        LlmClient = llmClient,
        Schema = MakeSchema(),
        Ct = CancellationToken.None,
    };

    public static EnrichmentJob MakeJob(
        EnrichmentType type = EnrichmentType.Description,
        string entityId = "e1") =>
        new(type, new EntityId(entityId), new SimulationSlotId(1));
}
