namespace TheCanonry.Engine.Tests.Systems;

using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Runtime;
using TheCanonry.Engine.Statistics;
using TheCanonry.Engine.Systems;
using TheCanonry.Engine.Templates;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

// =============================================================================
// TEST STUBS
// =============================================================================

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

file sealed class StubNameService : INameGenerationService
{
    public Task<string> GenerateAsync(
        EntityKind kind, string subtype, Prominence prominence,
        EntityTags tags, CultureId culture, string? context = null) =>
        Task.FromResult($"{kind}-{subtype}-name");
}

// =============================================================================
// TEST HELPERS
// =============================================================================

file static class Helpers
{
    internal static EngineConfig CreateMinimalConfig(IReadOnlyList<Era>? eras = null)
    {
        var schema = new DomainSchema
        {
            Id = "test-domain",
            Name = "Test Domain",
            Version = "1.0",
            EntityKinds = [],
            RelationshipKinds =
            [
                new RelationshipKindDefinition
                {
                    Kind = new RelationshipKind("allied_with"),
                    Description = "Alliance relationship",
                    Cullable = true,
                    DecayRate = "medium"
                },
                new RelationshipKindDefinition
                {
                    Kind = new RelationshipKind("rival_of"),
                    Description = "Rivalry relationship",
                    Cullable = true,
                    DecayRate = "fast"
                },
                new RelationshipKindDefinition
                {
                    Kind = new RelationshipKind("member_of"),
                    Description = "Membership relationship",
                    Cullable = false,
                    DecayRate = "none"
                }
            ],
            Cultures = []
        };

        return new EngineConfig(
            Schema: schema,
            Eras: eras ?? [],
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

    internal static Era CreateTestEra(
        string id,
        string name,
        string summary = "",
        IReadOnlyList<Condition>? exitConditions = null,
        IReadOnlyList<Condition>? entryConditions = null,
        EraId? nextEra = null,
        EraTransitionEffects? exitEffects = null,
        EraTransitionEffects? entryEffects = null)
    {
        return new Era(
            Id: new EraId(id),
            Name: name,
            Summary: summary,
            TemplateWeights: new Dictionary<string, double>(),
            SystemModifiers: new Dictionary<string, double>(),
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: exitConditions ?? [],
            EntryConditions: entryConditions ?? [],
            NextEra: nextEra,
            ExitEffects: exitEffects ?? EraTransitionEffects.Empty,
            EntryEffects: entryEffects ?? EraTransitionEffects.Empty);
    }

    internal static Entity CreateTestEntity(
        string id,
        string kind = "faction",
        string subtype = "merchant",
        string culture = "northern",
        int tick = 1,
        double prominence = 0.0)
    {
        var entity = new Entity(
            id: new EntityId(id),
            kind: new EntityKind(kind),
            subtype: subtype,
            name: $"Test {id}",
            culture: new CultureId(culture),
            eraId: new EraId("era-1"),
            coordinates: new SemanticCoordinates(50.0, 50.0, 50.0),
            createdBy: new ExecutionContext(tick, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: tick);

        if (Math.Abs(prominence) > 1e-9)
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
}

// =============================================================================
// ERA SPAWNER TESTS
// =============================================================================

public class EraSpawnerTests
{
    [Fact]
    public async Task EraSpawner_CreatesEraEntities()
    {
        var era1 = Helpers.CreateTestEra("era-1", "The Dawn");
        var era2 = Helpers.CreateTestEra("era-2", "The Dusk");
        var config = Helpers.CreateMinimalConfig([era1, era2]);
        var runtime = new WorldRuntime(config, randomSeed: 42);

        var spawner = new EraSpawner(
            new FrameworkSystemConfig("era_spawner", "Era Spawner", ""),
            runtime);

        var result = await spawner.ApplyAsync(1.0);

        // Should have created the first era entity
        var eraEntity = runtime.GetEntity(new EntityId("era-1"));
        Assert.NotNull(eraEntity);
        Assert.Equal(FrameworkPrimitives.EntityKinds.Era, eraEntity.Kind);
        Assert.Equal("The Dawn", eraEntity.Name);
        Assert.Equal(FrameworkPrimitives.Statuses.Current, eraEntity.Status);
        Assert.Contains("Started first era", result.Description);
    }

    [Fact]
    public async Task EraSpawner_CreatesOnlyFirstEra_LazyModel()
    {
        var era1 = Helpers.CreateTestEra("era-1", "The Dawn");
        var era2 = Helpers.CreateTestEra("era-2", "The Dusk");
        var config = Helpers.CreateMinimalConfig([era1, era2]);
        var runtime = new WorldRuntime(config, randomSeed: 42);

        var spawner = new EraSpawner(
            new FrameworkSystemConfig("era_spawner", "Era Spawner", ""),
            runtime);

        await spawner.ApplyAsync(1.0);

        // Only the first era should exist
        Assert.NotNull(runtime.GetEntity(new EntityId("era-1")));
        Assert.Null(runtime.GetEntity(new EntityId("era-2")));
    }

    [Fact]
    public async Task EraSpawner_SetsCurrentEraOnRuntime()
    {
        var era1 = Helpers.CreateTestEra("era-1", "The Dawn");
        var config = Helpers.CreateMinimalConfig([era1]);
        var runtime = new WorldRuntime(config, randomSeed: 42);

        var spawner = new EraSpawner(
            new FrameworkSystemConfig("era_spawner", "Era Spawner", ""),
            runtime);

        await spawner.ApplyAsync(1.0);

        Assert.NotNull(runtime.CurrentEra);
        Assert.Equal(new EraId("era-1"), runtime.CurrentEra.Id);
    }

    [Fact]
    public async Task EraSpawner_SkipsIfErasAlreadyExist()
    {
        var era1 = Helpers.CreateTestEra("era-1", "The Dawn");
        var config = Helpers.CreateMinimalConfig([era1]);
        var runtime = new WorldRuntime(config, randomSeed: 42);

        var spawner = new EraSpawner(
            new FrameworkSystemConfig("era_spawner", "Era Spawner", ""),
            runtime);

        // Run twice
        await spawner.ApplyAsync(1.0);
        var result2 = await spawner.ApplyAsync(1.0);

        // Second call should be a no-op
        Assert.Contains("already exist", result2.Description);
        // Only one era entity should exist
        var eras = runtime.FindEntities(EntityCriteria.Create(
            kind: FrameworkPrimitives.EntityKinds.Era, includeHistorical: true));
        Assert.Single(eras);
    }

    [Fact]
    public async Task EraSpawner_AppliesEntryEffects()
    {
        var era1 = Helpers.CreateTestEra("era-1", "The Dawn",
            entryEffects: new EraTransitionEffects([
                new ModifyPressureMutation("conflict", 10.0),
                new ModifyPressureMutation("trade", 5.0)
            ]));
        var config = Helpers.CreateMinimalConfig([era1]);
        var runtime = new WorldRuntime(config, randomSeed: 42);

        var spawner = new EraSpawner(
            new FrameworkSystemConfig("era_spawner", "Era Spawner", ""),
            runtime);

        var result = await spawner.ApplyAsync(1.0);

        Assert.Equal(10.0, result.PressureChanges["conflict"]);
        Assert.Equal(5.0, result.PressureChanges["trade"]);
    }

    [Fact]
    public async Task EraSpawner_EraEntityHasCorrectTags()
    {
        var era1 = Helpers.CreateTestEra("era-1", "The Dawn");
        var config = Helpers.CreateMinimalConfig([era1]);
        var runtime = new WorldRuntime(config, randomSeed: 42);

        var spawner = new EraSpawner(
            new FrameworkSystemConfig("era_spawner", "Era Spawner", ""),
            runtime);

        await spawner.ApplyAsync(1.0);

        var eraEntity = runtime.GetEntity(new EntityId("era-1"))!;
        Assert.True(eraEntity.Tags.GetBool(FrameworkPrimitives.Tags.Temporal));
        Assert.True(eraEntity.Tags.GetBool(FrameworkPrimitives.Tags.Era));
        Assert.Equal("era-1", eraEntity.Tags.GetString(FrameworkPrimitives.Tags.EraId));
    }
}

// =============================================================================
// ERA TRANSITION TESTS
// =============================================================================

public class EraTransitionTests
{
    [Fact]
    public async Task EraTransition_DetectsExitConditions()
    {
        // Exit condition: entity count of kind "faction" must be >= 3
        var era1 = Helpers.CreateTestEra("era-1", "The Dawn",
            exitConditions: [new EntityCountCondition("faction", "", "", 3, 100, 1.0)]);
        var era2 = Helpers.CreateTestEra("era-2", "The Dusk");
        var config = Helpers.CreateMinimalConfig([era1, era2]);
        var runtime = new WorldRuntime(config, randomSeed: 42);

        // Set up the first era
        var spawner = new EraSpawner(
            new FrameworkSystemConfig("era_spawner", "Era Spawner", ""),
            runtime);
        await spawner.ApplyAsync(1.0);

        // Add only 2 factions (below threshold)
        runtime.CreateEntity(Helpers.CreateTestEntity("f1", kind: "faction"));
        runtime.CreateEntity(Helpers.CreateTestEntity("f2", kind: "faction"));

        var transition = new EraTransition(
            new EraTransitionConfig("era_transition", "Era Transition", "", true, "recognized"),
            runtime);

        var result = await transition.ApplyAsync(1.0);

        // Should NOT transition yet
        Assert.Contains("persists", result.Description);
        Assert.Equal(FrameworkPrimitives.Statuses.Current,
            runtime.GetEntity(new EntityId("era-1"))!.Status);
    }

    [Fact]
    public async Task EraTransition_ActivatesNextEra()
    {
        // Exit condition: always pass (empty)
        var era1 = Helpers.CreateTestEra("era-1", "The Dawn", exitConditions: []);
        var era2 = Helpers.CreateTestEra("era-2", "The Dusk");
        var config = Helpers.CreateMinimalConfig([era1, era2]);
        var runtime = new WorldRuntime(config, randomSeed: 42);

        // Set up the first era
        var spawner = new EraSpawner(
            new FrameworkSystemConfig("era_spawner", "Era Spawner", ""),
            runtime);
        await spawner.ApplyAsync(1.0);

        var transition = new EraTransition(
            new EraTransitionConfig("era_transition", "Era Transition", "", false, "recognized"),
            runtime);

        var result = await transition.ApplyAsync(1.0);

        // Should transition to era-2
        Assert.Contains("Era transition", result.Description);
        Assert.Contains("The Dusk", result.Description);

        // Old era should be historical
        var oldEra = runtime.GetEntity(new EntityId("era-1"));
        Assert.NotNull(oldEra);
        Assert.Equal(FrameworkPrimitives.Statuses.Historical, oldEra.Status);

        // New era should exist and be current
        var newEra = runtime.GetEntity(new EntityId("era-2"));
        Assert.NotNull(newEra);
        Assert.Equal(FrameworkPrimitives.Statuses.Current, newEra.Status);

        // Runtime current era should be updated
        Assert.NotNull(runtime.CurrentEra);
        Assert.Equal(new EraId("era-2"), runtime.CurrentEra.Id);
    }

    [Fact]
    public async Task EraTransition_CreatesSupersedes_Relationship()
    {
        var era1 = Helpers.CreateTestEra("era-1", "The Dawn", exitConditions: []);
        var era2 = Helpers.CreateTestEra("era-2", "The Dusk");
        var config = Helpers.CreateMinimalConfig([era1, era2]);
        var runtime = new WorldRuntime(config, randomSeed: 42);

        var spawner = new EraSpawner(
            new FrameworkSystemConfig("era_spawner", "Era Spawner", ""),
            runtime);
        await spawner.ApplyAsync(1.0);

        var transition = new EraTransition(
            new EraTransitionConfig("era_transition", "Era Transition", "", false, "recognized"),
            runtime);

        var result = await transition.ApplyAsync(1.0);

        // Should have supersedes relationship: era-2 supersedes era-1
        Assert.True(result.RelationshipsAdded.Count >= 1);
        var supersedesRel = result.RelationshipsAdded[0];
        Assert.Equal(FrameworkPrimitives.RelationshipKinds.Supersedes.Value, supersedesRel.Kind);
        Assert.Equal("era-2", supersedesRel.Src);
        Assert.Equal("era-1", supersedesRel.Dst);
    }

    [Fact]
    public async Task EraTransition_CollectsExitAndEntryPressureEffects()
    {
        var era1 = Helpers.CreateTestEra("era-1", "The Dawn",
            exitConditions: [],
            exitEffects: new EraTransitionEffects([
                new ModifyPressureMutation("conflict", -5.0)
            ]));
        var era2 = Helpers.CreateTestEra("era-2", "The Dusk",
            entryEffects: new EraTransitionEffects([
                new ModifyPressureMutation("conflict", 10.0),
                new ModifyPressureMutation("trade", 3.0)
            ]));
        var config = Helpers.CreateMinimalConfig([era1, era2]);
        var runtime = new WorldRuntime(config, randomSeed: 42);

        var spawner = new EraSpawner(
            new FrameworkSystemConfig("era_spawner", "Era Spawner", ""),
            runtime);
        await spawner.ApplyAsync(1.0);

        var transition = new EraTransition(
            new EraTransitionConfig("era_transition", "Era Transition", "", false, "recognized"),
            runtime);

        var result = await transition.ApplyAsync(1.0);

        // Conflict: -5 (exit) + 10 (entry) = 5
        Assert.Equal(5.0, result.PressureChanges["conflict"]);
        Assert.Equal(3.0, result.PressureChanges["trade"]);
    }
}

// =============================================================================
// CONNECTION EVOLUTION TESTS
// =============================================================================

public class ConnectionEvolutionTests
{
    [Fact]
    public async Task ConnectionEvolution_StrengthensRelatedEntities()
    {
        var config = Helpers.CreateMinimalConfig();
        var runtime = new WorldRuntime(config, randomSeed: 42);

        // Create entities
        runtime.CreateEntity(Helpers.CreateTestEntity("f1", kind: "faction", prominence: 3.0));
        runtime.CreateEntity(Helpers.CreateTestEntity("f2", kind: "faction", prominence: 3.0));
        runtime.AddRelationship(Helpers.CreateTestRelationship("f1", "f2", "allied_with", 0.5));

        var systemConfig = new Dictionary<string, object?>
        {
            ["throttleChance"] = 1.0,
            ["selection.kind"] = "faction",
            ["rules"] = new List<object?>
            {
                new Dictionary<string, object?>
                {
                    ["condition.operator"] = ">=",
                    ["condition.threshold"] = 0.0,
                    ["probability"] = 1.0,
                    ["betweenMatching"] = false,
                    ["action"] = new Dictionary<string, object?>
                    {
                        ["type"] = "adjust_prominence",
                        ["delta"] = 0.5
                    }
                }
            }
        };

        var system = new ConnectionEvolution("conn_evo", "Connection Evolution",
            systemConfig, runtime);

        var result = await system.ApplyAsync(1.0);

        // Should have modified entities
        Assert.True(result.EntitiesModified.Count > 0);
        Assert.Contains("modified", result.Description);
    }

    [Fact]
    public async Task ConnectionEvolution_RemovesWeakRelationships_ViaThrottle()
    {
        var config = Helpers.CreateMinimalConfig();
        var runtime = new WorldRuntime(config, randomSeed: 42);

        runtime.CreateEntity(Helpers.CreateTestEntity("f1", kind: "faction"));
        runtime.CreateEntity(Helpers.CreateTestEntity("f2", kind: "faction"));

        var systemConfig = new Dictionary<string, object?>
        {
            ["throttleChance"] = 0.0  // Always throttled
        };

        var system = new ConnectionEvolution("conn_evo", "Connection Evolution",
            systemConfig, runtime);

        var result = await system.ApplyAsync(1.0);

        // Should be throttled
        Assert.Contains("throttled", result.Description);
    }
}

// =============================================================================
// RELATIONSHIP MAINTENANCE TESTS
// =============================================================================

public class RelationshipMaintenanceTests
{
    [Fact]
    public async Task RelationshipMaintenance_AppliesDecay()
    {
        var config = Helpers.CreateMinimalConfig();
        var runtime = new WorldRuntime(config, randomSeed: 42);

        // Create entities with early tick so they're past grace period
        runtime.CreateEntity(Helpers.CreateTestEntity("f1", tick: 1));
        runtime.CreateEntity(Helpers.CreateTestEntity("f2", tick: 1));
        runtime.AddRelationship(Helpers.CreateTestRelationship("f1", "f2", "allied_with", 0.5, tick: 1));

        // Advance tick well past grace period
        runtime.CurrentTick = 100;

        var maintenanceConfig = new RelationshipMaintenanceConfig(
            Id: "rel_maintenance",
            Name: "Relationship Maintenance",
            Description: "",
            MaintenanceFrequency: 1, // Run every tick
            CullThreshold: 0.15,
            GracePeriod: 20,
            ReinforcementBonus: 0.02,
            MaxStrength: 1.0,
            ProximityRelationshipKinds: []);

        var system = new RelationshipMaintenance(maintenanceConfig, runtime);

        var result = await system.ApplyAsync(1.0);

        // Should have decayed relationships
        Assert.Contains("decayed", result.Description);
    }

    [Fact]
    public async Task RelationshipMaintenance_CullsBelowThreshold()
    {
        var config = Helpers.CreateMinimalConfig();
        var runtime = new WorldRuntime(config, randomSeed: 42);

        // Create entities with early tick
        runtime.CreateEntity(Helpers.CreateTestEntity("f1", tick: 1));
        runtime.CreateEntity(Helpers.CreateTestEntity("f2", tick: 1));

        // Create a very weak relationship
        runtime.AddRelationship(Helpers.CreateTestRelationship("f1", "f2", "allied_with", 0.05, tick: 1));

        // Advance past grace period
        runtime.CurrentTick = 100;

        var maintenanceConfig = new RelationshipMaintenanceConfig(
            Id: "rel_maintenance",
            Name: "Relationship Maintenance",
            Description: "",
            MaintenanceFrequency: 1,
            CullThreshold: 0.15,
            GracePeriod: 20,
            ReinforcementBonus: 0.02,
            MaxStrength: 1.0,
            ProximityRelationshipKinds: []);

        var system = new RelationshipMaintenance(maintenanceConfig, runtime);

        var result = await system.ApplyAsync(1.0);

        // Should have archived the weak relationship
        Assert.Contains("archived", result.Description);
    }

    [Fact]
    public async Task RelationshipMaintenance_RespectsGracePeriod()
    {
        var config = Helpers.CreateMinimalConfig();
        var runtime = new WorldRuntime(config, randomSeed: 42);

        // Create entities at tick 1
        runtime.CreateEntity(Helpers.CreateTestEntity("f1", tick: 1));
        runtime.CreateEntity(Helpers.CreateTestEntity("f2", tick: 1));
        runtime.AddRelationship(Helpers.CreateTestRelationship("f1", "f2", "allied_with", 0.05, tick: 1));

        // Tick within grace period (grace = 20)
        runtime.CurrentTick = 10;

        var maintenanceConfig = new RelationshipMaintenanceConfig(
            Id: "rel_maintenance",
            Name: "Relationship Maintenance",
            Description: "",
            MaintenanceFrequency: 1,
            CullThreshold: 0.15,
            GracePeriod: 20,
            ReinforcementBonus: 0.02,
            MaxStrength: 1.0,
            ProximityRelationshipKinds: []);

        var system = new RelationshipMaintenance(maintenanceConfig, runtime);

        var result = await system.ApplyAsync(1.0);

        // Should NOT have archived - within grace period
        Assert.DoesNotContain("archived", result.Description);

        // Relationship should still be active
        var rels = runtime.GetRelationships();
        Assert.Single(rels);
        Assert.Equal(FrameworkPrimitives.Statuses.Active, rels[0].Status);
    }

    [Fact]
    public async Task RelationshipMaintenance_NeverDecaysFrameworkRelationships()
    {
        var config = Helpers.CreateMinimalConfig();
        var runtime = new WorldRuntime(config, randomSeed: 42);

        // Create era entities
        runtime.CreateEntity(Helpers.CreateTestEntity("era-1", kind: "era", tick: 1));
        runtime.CreateEntity(Helpers.CreateTestEntity("era-2", kind: "era", tick: 1));

        // Create a supersedes relationship (framework)
        runtime.AddRelationship(Helpers.CreateTestRelationship(
            "era-2", "era-1", "supersedes", 0.05, tick: 1));

        runtime.CurrentTick = 100;

        var maintenanceConfig = new RelationshipMaintenanceConfig(
            Id: "rel_maintenance",
            Name: "Relationship Maintenance",
            Description: "",
            MaintenanceFrequency: 1,
            CullThreshold: 0.15,
            GracePeriod: 20,
            ReinforcementBonus: 0.02,
            MaxStrength: 1.0,
            ProximityRelationshipKinds: []);

        var system = new RelationshipMaintenance(maintenanceConfig, runtime);

        var result = await system.ApplyAsync(1.0);

        // Framework relationship should still be active (never archived)
        var rels = runtime.GetRelationships();
        Assert.Single(rels);
        Assert.Equal(FrameworkPrimitives.Statuses.Active, rels[0].Status);
    }

    [Fact]
    public async Task RelationshipMaintenance_OnlyRunsOnFrequencyTicks()
    {
        var config = Helpers.CreateMinimalConfig();
        var runtime = new WorldRuntime(config, randomSeed: 42);

        runtime.CurrentTick = 3; // Not divisible by 5

        var maintenanceConfig = new RelationshipMaintenanceConfig(
            Id: "rel_maintenance",
            Name: "Relationship Maintenance",
            Description: "",
            MaintenanceFrequency: 5,
            CullThreshold: 0.15,
            GracePeriod: 20,
            ReinforcementBonus: 0.02,
            MaxStrength: 1.0,
            ProximityRelationshipKinds: []);

        var system = new RelationshipMaintenance(maintenanceConfig, runtime);

        var result = await system.ApplyAsync(1.0);

        Assert.Contains("dormant", result.Description);
    }
}

// =============================================================================
// THRESHOLD TRIGGER TESTS
// =============================================================================

public class ThresholdTriggerTests
{
    [Fact]
    public async Task ThresholdTrigger_FiresOnConditionMet()
    {
        var config = Helpers.CreateMinimalConfig();
        var runtime = new WorldRuntime(config, randomSeed: 42);

        // Create entities with prominent status
        var e1 = Helpers.CreateTestEntity("f1", kind: "faction", prominence: 3.0);
        runtime.CreateEntity(e1);

        // Condition: entity must have prominence >= recognized (2.0)
        var systemConfig = new Dictionary<string, object?>
        {
            ["throttleChance"] = 1.0,
            ["selection.kind"] = "faction",
            ["clusterMode"] = "individual",
            ["minClusterSize"] = 1,
            ["conditions"] = new List<Condition>
            {
                new ProminenceCondition("recognized", "mythic")
            },
            ["actions"] = new List<Mutation>
            {
                new SetTagMutation("$self", "power_holder", "true", "")
            },
            ["pressureChanges"] = new Dictionary<string, object?> { ["influence"] = 2.0 }
        };

        var system = new ThresholdTrigger("threshold", "Power Check",
            systemConfig, runtime);

        var result = await system.ApplyAsync(1.0);

        // Should have triggered
        Assert.Contains("trigger", result.Description);
        Assert.True(result.EntitiesModified.Count > 0);
    }

    [Fact]
    public async Task ThresholdTrigger_ModifiesPressures()
    {
        var config = Helpers.CreateMinimalConfig();
        var runtime = new WorldRuntime(config, randomSeed: 42);

        var e1 = Helpers.CreateTestEntity("f1", kind: "faction", prominence: 3.0);
        runtime.CreateEntity(e1);

        var systemConfig = new Dictionary<string, object?>
        {
            ["throttleChance"] = 1.0,
            ["selection.kind"] = "faction",
            ["clusterMode"] = "individual",
            ["minClusterSize"] = 1,
            ["conditions"] = new List<Condition>
            {
                new ProminenceCondition("recognized", "mythic")
            },
            ["actions"] = new List<Mutation>
            {
                new SetTagMutation("$self", "tagged", "true", "")
            },
            ["pressureChanges"] = new Dictionary<string, object?> { ["conflict"] = 5.0 }
        };

        var system = new ThresholdTrigger("threshold", "Conflict Check",
            systemConfig, runtime);

        var result = await system.ApplyAsync(1.0);

        Assert.True(result.PressureChanges.ContainsKey("conflict"));
        Assert.Equal(5.0, result.PressureChanges["conflict"]);
    }

    [Fact]
    public async Task ThresholdTrigger_SkipsWhenCooldownTagPresent()
    {
        var config = Helpers.CreateMinimalConfig();
        var runtime = new WorldRuntime(config, randomSeed: 42);

        // Create entity with cooldown tag
        var e1 = Helpers.CreateTestEntity("f1", kind: "faction", prominence: 3.0);
        e1.Tags.Set("on_cooldown", true);
        runtime.CreateEntity(e1);

        var systemConfig = new Dictionary<string, object?>
        {
            ["throttleChance"] = 1.0,
            ["cooldownTag"] = "on_cooldown",
            ["selection.kind"] = "faction",
            ["clusterMode"] = "individual",
            ["minClusterSize"] = 1,
            ["conditions"] = new List<Condition>
            {
                new AlwaysCondition()
            },
            ["actions"] = new List<Mutation>
            {
                new SetTagMutation("$self", "triggered", "true", "")
            }
        };

        var system = new ThresholdTrigger("threshold", "Cooldown Check",
            systemConfig, runtime);

        var result = await system.ApplyAsync(1.0);

        // Should have no matches due to cooldown
        Assert.Contains("no matches", result.Description);
    }

    [Fact]
    public async Task ThresholdTrigger_NoMatchesWhenConditionNotMet()
    {
        var config = Helpers.CreateMinimalConfig();
        var runtime = new WorldRuntime(config, randomSeed: 42);

        // Create entity with low prominence
        runtime.CreateEntity(Helpers.CreateTestEntity("f1", kind: "faction", prominence: 0.5));

        var systemConfig = new Dictionary<string, object?>
        {
            ["throttleChance"] = 1.0,
            ["selection.kind"] = "faction",
            ["clusterMode"] = "individual",
            ["minClusterSize"] = 1,
            ["conditions"] = new List<Condition>
            {
                new ProminenceCondition("renowned", "mythic")
            },
            ["actions"] = new List<Mutation>
            {
                new SetTagMutation("$self", "triggered", "true", "")
            }
        };

        var system = new ThresholdTrigger("threshold", "High Prominence Check",
            systemConfig, runtime);

        var result = await system.ApplyAsync(1.0);

        Assert.Contains("no matches", result.Description);
    }
}
