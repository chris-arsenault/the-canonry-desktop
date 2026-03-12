namespace TheCanonry.Engine.Tests.Runtime;

using TheCanonry.Engine.Coordinates;
using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Runtime;
using TheCanonry.Engine.Selection;
using TheCanonry.Engine.Statistics;
using TheCanonry.Engine.Templates;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

// =============================================================================
// TEST STUBS
// =============================================================================

/// <summary>No-op emitter for tests.</summary>
file sealed class NullEmitter : ISimulationEmitter
{
    public void Progress(ProgressPayload payload) { }
    public void Log(LogLevel level, string message, IReadOnlyDictionary<string, object?>? context = null) { }
    public void EpochStart(EpochStartPayload payload) { }
    public void SystemAction(SystemActionPayload payload) { }
    public void EraTransition(EraId fromEra, string fromName, EraId toEra, string toName, int tick) { }
    public void Error(ErrorPayload payload) { }
    public void Complete() { }
}

/// <summary>Stub name generation that returns a deterministic name.</summary>
file sealed class StubNameService : INameGenerationService
{
    public Task<string> GenerateAsync(
        EntityKind kind, string subtype, Prominence prominence,
        EntityTags tags, CultureId culture, string? context = null) =>
        Task.FromResult($"{kind}-{subtype}-name");
}

// =============================================================================
// TESTS
// =============================================================================

public class WorldRuntimeTests
{
    // =========================================================================
    // HELPERS
    // =========================================================================

    private static EngineConfig CreateMinimalConfig()
    {
        var schema = new DomainSchema
        {
            Id = "test-domain",
            Name = "Test Domain",
            Version = "1.0",
            EntityKinds = [],
            RelationshipKinds = [],
            Cultures = []
        };

        return new EngineConfig(
            Schema: schema,
            Eras: [],
            Templates: [],
            Systems: [],
            Pressures: [],
            TicksPerEpoch: 10,
            MaxEpochs: 5,
            MaxTicks: 50,
            MaxRelationshipsPerType: 100,
            RelationshipBudget: new RelationshipBudget(20, 50),
            ScaleFactor: 1.0,
            DefaultMinDistance: 5.0,
            PressureDeltaSmoothing: 0.5,
            DistributionTargets: new DistributionTargets(),
            NameService: new StubNameService(),
            SeedRelationships: [],
            Emitter: new NullEmitter(),
            NarrativeConfig: new NarrativeConfig(true, 0.0));
    }

    private static Entity CreateTestEntity(
        string id,
        string kind = "faction",
        string subtype = "merchant",
        string culture = "northern",
        int tick = 1)
    {
        return new Entity(
            id: new EntityId(id),
            kind: new EntityKind(kind),
            subtype: subtype,
            name: $"Test {id}",
            culture: new CultureId(culture),
            eraId: new EraId("era-1"),
            coordinates: new SemanticCoordinates(50.0, 50.0, 50.0),
            createdBy: new ExecutionContext(tick, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: tick);
    }

    private static Relationship CreateTestRelationship(
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

    // =========================================================================
    // CONSTRUCTION
    // =========================================================================

    [Fact]
    public void CanCreate_WithMinimalConfig()
    {
        var config = CreateMinimalConfig();
        var runtime = new WorldRuntime(config);

        Assert.Equal(0, runtime.CurrentTick);
        Assert.Equal(0, runtime.CurrentEpoch);
        Assert.Null(runtime.CurrentEra);
        Assert.Same(config, runtime.Config);
        Assert.Same(config.Schema, runtime.Schema);
    }

    [Fact]
    public void CanCreate_WithExplicitGraph()
    {
        var config = CreateMinimalConfig();
        var graph = new GraphStore();
        var entity = CreateTestEntity("pre-existing");
        graph.CreateEntity(entity);

        var runtime = new WorldRuntime(config, graph);

        Assert.True(runtime.HasEntity(new EntityId("pre-existing")));
    }

    [Fact]
    public void CanCreate_WithDeterministicSeed()
    {
        var config = CreateMinimalConfig();
        var runtime1 = new WorldRuntime(config, randomSeed: 42);
        var runtime2 = new WorldRuntime(config, randomSeed: 42);

        var seq1 = Enumerable.Range(0, 10).Select(_ => runtime1.NextRandomDouble()).ToList();
        var seq2 = Enumerable.Range(0, 10).Select(_ => runtime2.NextRandomDouble()).ToList();

        Assert.Equal(seq1, seq2);
    }

    // =========================================================================
    // ENTITY OPERATIONS
    // =========================================================================

    [Fact]
    public void CanAddAndRetrieveEntities()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        var entity = CreateTestEntity("faction-1");

        runtime.CreateEntity(entity);
        var retrieved = runtime.GetEntity(new EntityId("faction-1"));

        Assert.NotNull(retrieved);
        Assert.Equal(new EntityId("faction-1"), retrieved.Id);
        Assert.Equal("Test faction-1", retrieved.Name);
        Assert.True(runtime.HasEntity(new EntityId("faction-1")));
        Assert.Equal(1, runtime.GetEntityCount());
    }

    [Fact]
    public void FindEntities_DelegatesToGraph()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        runtime.CreateEntity(CreateTestEntity("f1", kind: "faction"));
        runtime.CreateEntity(CreateTestEntity("n1", kind: "npc"));
        runtime.CreateEntity(CreateTestEntity("f2", kind: "faction"));

        var factions = runtime.FindEntities(EntityCriteria.Create(kind: new EntityKind("faction")));

        Assert.Equal(2, factions.Count);
    }

    [Fact]
    public void UpdateEntity_MutatesViaCallback()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        runtime.CreateEntity(CreateTestEntity("faction-1"));
        runtime.CurrentTick = 10;

        var updated = runtime.UpdateEntity(new EntityId("faction-1"), e =>
            e.SetName("Updated Name", 10));

        Assert.True(updated);
        Assert.Equal("Updated Name", runtime.GetEntity(new EntityId("faction-1"))!.Name);
    }

    [Fact]
    public void DeleteEntity_RemovesFromGraph()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        runtime.CreateEntity(CreateTestEntity("faction-1"));

        Assert.True(runtime.DeleteEntity(new EntityId("faction-1")));
        Assert.False(runtime.HasEntity(new EntityId("faction-1")));
        Assert.Equal(0, runtime.GetEntityCount());
    }

    // =========================================================================
    // RELATIONSHIP OPERATIONS
    // =========================================================================

    [Fact]
    public void CanAddAndRetrieveRelationships()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        runtime.CreateEntity(CreateTestEntity("f1"));
        runtime.CreateEntity(CreateTestEntity("f2"));

        var rel = CreateTestRelationship("f1", "f2");
        var added = runtime.AddRelationship(rel);

        Assert.True(added);

        var rels = runtime.GetRelationships();
        Assert.Single(rels);
        Assert.Equal(new EntityId("f1"), rels[0].SourceId);
        Assert.Equal(new EntityId("f2"), rels[0].TargetId);

        Assert.True(runtime.HasRelationship(new EntityId("f1"), new EntityId("f2")));
        Assert.True(runtime.HasRelationship(
            new EntityId("f1"), new EntityId("f2"),
            new RelationshipKind("allied_with")));
    }

    [Fact]
    public void GetEntityRelationships_ByDirection()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        runtime.CreateEntity(CreateTestEntity("f1"));
        runtime.CreateEntity(CreateTestEntity("f2"));
        runtime.CreateEntity(CreateTestEntity("f3"));

        runtime.AddRelationship(CreateTestRelationship("f1", "f2"));
        runtime.AddRelationship(CreateTestRelationship("f3", "f1"));

        var asSource = runtime.GetEntityRelationships(new EntityId("f1"), Direction.Source);
        Assert.Single(asSource);

        var asTarget = runtime.GetEntityRelationships(new EntityId("f1"), Direction.Target);
        Assert.Single(asTarget);

        var both = runtime.GetEntityRelationships(new EntityId("f1"), Direction.Both);
        Assert.Equal(2, both.Count);
    }

    [Fact]
    public void RemoveRelationship_Works()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        runtime.CreateEntity(CreateTestEntity("f1"));
        runtime.CreateEntity(CreateTestEntity("f2"));
        runtime.AddRelationship(CreateTestRelationship("f1", "f2", "allied_with"));

        Assert.True(runtime.RemoveRelationship(
            new EntityId("f1"), new EntityId("f2"), new RelationshipKind("allied_with")));
        Assert.Empty(runtime.GetRelationships());
    }

    // =========================================================================
    // PRESSURE MANAGEMENT
    // =========================================================================

    [Fact]
    public void PressureManagement_GetSetModify()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());

        // Initial pressure is 0
        Assert.Equal(0.0, runtime.GetPressure("conflict"));

        // Set pressure
        runtime.SetPressure("conflict", 50.0);
        Assert.Equal(50.0, runtime.GetPressure("conflict"));

        // Modify pressure
        runtime.ModifyPressure("conflict", 10.0);
        Assert.Equal(60.0, runtime.GetPressure("conflict"));

        // Modify with negative delta
        runtime.ModifyPressure("conflict", -25.0);
        Assert.Equal(35.0, runtime.GetPressure("conflict"));

        // Get all pressures
        var all = runtime.GetAllPressures();
        Assert.Single(all);
        Assert.Equal(35.0, all["conflict"]);
    }

    [Fact]
    public void PressureManagement_ClampsToRange()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());

        // Upper bound
        runtime.SetPressure("conflict", 200.0);
        Assert.Equal(100.0, runtime.GetPressure("conflict"));

        // Lower bound
        runtime.SetPressure("conflict", -200.0);
        Assert.Equal(-100.0, runtime.GetPressure("conflict"));

        // Modify past upper bound
        runtime.SetPressure("conflict", 90.0);
        runtime.ModifyPressure("conflict", 20.0);
        Assert.Equal(100.0, runtime.GetPressure("conflict"));
    }

    // =========================================================================
    // TICK / EPOCH TRACKING
    // =========================================================================

    [Fact]
    public void TickAndEpoch_TrackCorrectly()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig()); // TicksPerEpoch = 10

        Assert.Equal(0, runtime.CurrentTick);
        Assert.Equal(0, runtime.CurrentEpoch);

        // Advance to tick 5
        for (var i = 0; i < 5; i++)
            runtime.AdvanceTick();

        Assert.Equal(5, runtime.CurrentTick);
        Assert.Equal(0, runtime.CurrentEpoch); // Still epoch 0 (5 / 10 = 0)

        // Advance to tick 10
        for (var i = 0; i < 5; i++)
            runtime.AdvanceTick();

        Assert.Equal(10, runtime.CurrentTick);
        Assert.Equal(1, runtime.CurrentEpoch); // Epoch 1 (10 / 10 = 1)

        // Direct set
        runtime.CurrentTick = 25;
        Assert.Equal(25, runtime.CurrentTick);
        Assert.Equal(2, runtime.CurrentEpoch); // 25 / 10 = 2
    }

    // =========================================================================
    // ERA MANAGEMENT
    // =========================================================================

    [Fact]
    public void EraManagement_SetAndGetCurrentEra()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        Assert.Null(runtime.CurrentEra);

        var era = new Era(
            Id: new EraId("golden-age"),
            Name: "The Golden Age",
            Summary: "An age of prosperity",
            TemplateWeights: new Dictionary<string, double>(),
            SystemModifiers: new Dictionary<string, double>(),
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        runtime.CurrentEra = era;

        Assert.NotNull(runtime.CurrentEra);
        Assert.Equal(new EraId("golden-age"), runtime.CurrentEra.Id);
        Assert.Equal("The Golden Age", runtime.CurrentEra.Name);
    }

    // =========================================================================
    // POPULATION TRACKER INTEGRATION
    // =========================================================================

    [Fact]
    public void PopulationTracker_UpdatesFromGraphState()
    {
        var config = CreateMinimalConfig() with
        {
            DistributionTargets = new DistributionTargets
            {
                Entities = new Dictionary<string, Dictionary<string, int>>
                {
                    ["faction"] = new() { ["merchant"] = 5 }
                }
            }
        };

        var runtime = new WorldRuntime(config);
        runtime.CreateEntity(CreateTestEntity("f1", kind: "faction", subtype: "merchant"));
        runtime.CreateEntity(CreateTestEntity("f2", kind: "faction", subtype: "merchant"));
        runtime.CreateEntity(CreateTestEntity("f3", kind: "faction", subtype: "merchant"));

        runtime.UpdatePopulationMetrics();
        var metrics = runtime.GetPopulationMetrics();

        Assert.True(metrics.Entities.ContainsKey("faction:merchant"));
        Assert.Equal(3, metrics.Entities["faction:merchant"].Count);
        Assert.Equal(5, metrics.Entities["faction:merchant"].Target);

        var summary = runtime.GetPopulationSummary();
        Assert.Equal(3, summary.TotalEntities);
    }

    // =========================================================================
    // ENTITY ARCHIVAL DELEGATION
    // =========================================================================

    [Fact]
    public void ArchiveEntity_MarksAsHistorical()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        runtime.CreateEntity(CreateTestEntity("f1"));
        runtime.CreateEntity(CreateTestEntity("f2"));
        runtime.AddRelationship(CreateTestRelationship("f1", "f2"));

        runtime.ArchiveEntity(new EntityId("f1"));

        var entity = runtime.GetEntity(new EntityId("f1"));
        Assert.NotNull(entity);
        Assert.Equal(FrameworkPrimitives.Statuses.Historical, entity.Status);

        // Entity should be excluded from default queries
        Assert.Equal(1, runtime.GetEntityCount());
        Assert.Equal(2, runtime.GetEntityCount(includeHistorical: true));
    }

    [Fact]
    public void TransferRelationships_DelegatesToArchival()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        runtime.CreateEntity(CreateTestEntity("source-1"));
        runtime.CreateEntity(CreateTestEntity("source-2"));
        runtime.CreateEntity(CreateTestEntity("target"));
        runtime.CreateEntity(CreateTestEntity("other"));

        runtime.AddRelationship(CreateTestRelationship("source-1", "other", "allied_with"));
        runtime.AddRelationship(CreateTestRelationship("source-2", "other", "rival_of"));

        var transferred = runtime.TransferRelationships(
            [new EntityId("source-1"), new EntityId("source-2")],
            new EntityId("target"));

        Assert.Equal(2, transferred);

        // New relationships should point from target to other
        Assert.True(runtime.HasRelationship(
            new EntityId("target"), new EntityId("other"),
            new RelationshipKind("allied_with")));
        Assert.True(runtime.HasRelationship(
            new EntityId("target"), new EntityId("other"),
            new RelationshipKind("rival_of")));
    }

    [Fact]
    public void CreatePartOfRelationships_DelegatesToArchival()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        runtime.CreateEntity(CreateTestEntity("member-1"));
        runtime.CreateEntity(CreateTestEntity("member-2"));
        runtime.CreateEntity(CreateTestEntity("container"));

        var created = runtime.CreatePartOfRelationships(
            [new EntityId("member-1"), new EntityId("member-2")],
            new EntityId("container"));

        Assert.Equal(2, created);

        Assert.True(runtime.HasRelationship(
            new EntityId("member-1"), new EntityId("container"),
            FrameworkPrimitives.RelationshipKinds.PartOf));
        Assert.True(runtime.HasRelationship(
            new EntityId("member-2"), new EntityId("container"),
            FrameworkPrimitives.RelationshipKinds.PartOf));
    }

    // =========================================================================
    // TARGET SELECTION DELEGATION
    // =========================================================================

    [Fact]
    public void SelectTargets_DelegatesToSelector()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        runtime.CreateEntity(CreateTestEntity("f1", kind: "faction"));
        runtime.CreateEntity(CreateTestEntity("f2", kind: "faction"));
        runtime.CreateEntity(CreateTestEntity("f3", kind: "faction"));
        runtime.CreateEntity(CreateTestEntity("n1", kind: "npc"));

        var result = runtime.SelectTargets(new EntityKind("faction"), 2);

        Assert.Equal(2, result.Existing.Count);
        Assert.All(result.Existing, e => Assert.Equal(new EntityKind("faction"), e.Kind));
        Assert.Equal(3, result.Diagnostics.CandidatesEvaluated);
    }

    // =========================================================================
    // RANDOM NUMBER GENERATION
    // =========================================================================

    [Fact]
    public void RandomGeneration_RespectsRange()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig(), randomSeed: 12345);

        for (var i = 0; i < 100; i++)
        {
            var intVal = runtime.NextRandomInt(10);
            Assert.InRange(intVal, 0, 9);

            var dblVal = runtime.NextRandomDouble();
            Assert.InRange(dblVal, 0.0, 1.0);
        }
    }

    // =========================================================================
    // LOGGING DELEGATION
    // =========================================================================

    [Fact]
    public void Log_DelegatesToEmitter()
    {
        var emitter = new RecordingEmitter();
        var config = CreateMinimalConfig() with { Emitter = emitter };
        var runtime = new WorldRuntime(config);

        runtime.Log(LogLevel.Info, "Test message");
        runtime.Log(LogLevel.Warn, "Warning message",
            new Dictionary<string, object?> { ["key"] = "value" });

        Assert.Equal(2, emitter.LogMessages.Count);
        Assert.Equal(LogLevel.Info, emitter.LogMessages[0].Level);
        Assert.Equal("Test message", emitter.LogMessages[0].Message);
        Assert.Equal(LogLevel.Warn, emitter.LogMessages[1].Level);
    }

    // =========================================================================
    // CONNECTED ENTITIES
    // =========================================================================

    [Fact]
    public void GetConnectedEntities_TraversesRelationships()
    {
        var runtime = new WorldRuntime(CreateMinimalConfig());
        runtime.CreateEntity(CreateTestEntity("f1"));
        runtime.CreateEntity(CreateTestEntity("f2"));
        runtime.CreateEntity(CreateTestEntity("f3"));

        runtime.AddRelationship(CreateTestRelationship("f1", "f2", "allied_with"));
        runtime.AddRelationship(CreateTestRelationship("f3", "f1", "rival_of"));

        var connected = runtime.GetConnectedEntities(new EntityId("f1"));
        Assert.Equal(2, connected.Count);

        var byKind = runtime.GetConnectedEntities(
            new EntityId("f1"), new RelationshipKind("allied_with"));
        Assert.Single(byKind);
        Assert.Equal(new EntityId("f2"), byKind[0].Id);
    }
}

// =============================================================================
// RECORDING EMITTER (for verifying log delegation)
// =============================================================================

file sealed class RecordingEmitter : ISimulationEmitter
{
    public List<(LogLevel Level, string Message)> LogMessages { get; } = [];

    public void Progress(ProgressPayload payload) { }

    public void Log(LogLevel level, string message, IReadOnlyDictionary<string, object?>? context = null) =>
        LogMessages.Add((level, message));

    public void EpochStart(EpochStartPayload payload) { }
    public void SystemAction(SystemActionPayload payload) { }
    public void EraTransition(EraId fromEra, string fromName, EraId toEra, string toName, int tick) { }
    public void Error(ErrorPayload payload) { }
    public void Complete() { }
}
