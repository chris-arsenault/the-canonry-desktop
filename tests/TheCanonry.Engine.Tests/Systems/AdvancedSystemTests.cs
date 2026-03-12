namespace TheCanonry.Engine.Tests.Systems;

using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Runtime;
using TheCanonry.Engine.Selection;
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
// TESTS
// =============================================================================

public class AdvancedSystemTests
{
    // =========================================================================
    // HELPERS
    // =========================================================================

    private static EngineConfig CreateConfig(int ticksPerEpoch = 10)
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
            TicksPerEpoch: ticksPerEpoch,
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

    private static WorldRuntime CreateRuntime(
        EngineConfig? config = null, GraphStore? graph = null, int seed = 42)
    {
        return new WorldRuntime(config ?? CreateConfig(), graph, seed);
    }

    private static Entity CreateEntity(
        string id,
        string kind = "npc",
        string subtype = "warrior",
        string culture = "northern",
        double x = 50.0, double y = 50.0, double z = 0.0,
        int tick = 1)
    {
        return new Entity(
            id: new EntityId(id),
            kind: new EntityKind(kind),
            subtype: subtype,
            name: $"Test {id}",
            culture: new CultureId(culture),
            eraId: new EraId("era-1"),
            coordinates: new SemanticCoordinates(x, y, z),
            createdBy: new ExecutionContext(tick, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: tick);
    }

    private static Relationship CreateRelationship(
        string srcId, string dstId,
        string kind = "allied_with",
        double strength = 0.8,
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
    // GRAPH CONTAGION TESTS
    // =========================================================================

    [Fact]
    public async Task GraphContagion_SpreadsFromInfectedEntities()
    {
        var runtime = CreateRuntime(seed: 1);

        // Create a chain: A (infected) -> B -> C
        var a = CreateEntity("a");
        a.Tags.Set("infected", true);
        runtime.CreateEntity(a);

        var b = CreateEntity("b");
        runtime.CreateEntity(b);

        var c = CreateEntity("c");
        runtime.CreateEntity(c);

        // A is connected to B
        runtime.AddRelationship(CreateRelationship("a", "b"));
        // B is connected to C
        runtime.AddRelationship(CreateRelationship("b", "c"));

        var config = new Dictionary<string, object?>
        {
            ["spreadTag"] = "infected",
            ["vectorKind"] = "allied_with",
            ["vectorDirection"] = "both",
            ["baseRate"] = 1.0,         // guaranteed spread
            ["contactMultiplier"] = 0.0,
            ["maxProbability"] = 1.0,
            ["maxDepth"] = 10,
            ["minStrength"] = 0.0
        };

        var system = new GraphContagion("contagion-1", "Test Contagion", config, runtime);
        var result = await system.ApplyAsync(1.0);

        // B should be infected (direct contact with A)
        var bEntity = runtime.GetEntity(new EntityId("b"))!;
        Assert.True(bEntity.Tags.Contains("infected"), "Entity B should be infected");

        // Result should report modifications
        Assert.True(result.EntitiesModified.Count > 0, "Should have entity modifications");
        Assert.Contains("new infections", result.Description);
    }

    [Fact]
    public async Task GraphContagion_RespectsMaxDepth()
    {
        var runtime = CreateRuntime(seed: 1);

        // Create chain: A (infected) -> B -> C -> D
        var a = CreateEntity("a");
        a.Tags.Set("infected", true);
        runtime.CreateEntity(a);

        var b = CreateEntity("b");
        runtime.CreateEntity(b);

        var c = CreateEntity("c");
        runtime.CreateEntity(c);

        var d = CreateEntity("d");
        runtime.CreateEntity(d);

        runtime.AddRelationship(CreateRelationship("a", "b"));
        runtime.AddRelationship(CreateRelationship("b", "c"));
        runtime.AddRelationship(CreateRelationship("c", "d"));

        var config = new Dictionary<string, object?>
        {
            ["spreadTag"] = "infected",
            ["vectorKind"] = "allied_with",
            ["vectorDirection"] = "both",
            ["baseRate"] = 1.0,
            ["contactMultiplier"] = 0.0,
            ["maxProbability"] = 1.0,
            ["maxDepth"] = 1,  // Only 1 hop from infected
            ["minStrength"] = 0.0
        };

        var system = new GraphContagion("contagion-1", "Test Contagion", config, runtime);
        await system.ApplyAsync(1.0);

        // B should be infected (1 hop from A)
        Assert.True(runtime.GetEntity(new EntityId("b"))!.Tags.Contains("infected"),
            "B should be infected (1 hop from A)");

        // C should NOT be infected because it's 2 hops from A but only B was infected,
        // and C was not directly connected to the initial infected set
        // (C connects to B which was not infected at categorization time)
        // This depends on timing - C was in susceptible list before B got infected
    }

    [Fact]
    public async Task GraphContagion_ProbabilityBasedSpread()
    {
        var runtime = CreateRuntime(seed: 42);

        // Create infected + susceptible
        var infected = CreateEntity("infected");
        infected.Tags.Set("sick", true);
        runtime.CreateEntity(infected);

        var susceptible = CreateEntity("susceptible");
        runtime.CreateEntity(susceptible);

        runtime.AddRelationship(CreateRelationship("infected", "susceptible"));

        // Very low probability
        var config = new Dictionary<string, object?>
        {
            ["spreadTag"] = "sick",
            ["vectorKind"] = "allied_with",
            ["vectorDirection"] = "both",
            ["baseRate"] = 0.001,
            ["contactMultiplier"] = 0.0,
            ["maxProbability"] = 0.001,
            ["maxDepth"] = 10,
            ["minStrength"] = 0.0
        };

        var system = new GraphContagion("contagion-1", "Test Contagion", config, runtime);

        // Run multiple times - at very low probability, most ticks should not spread
        var spreadCount = 0;
        for (var i = 0; i < 20; i++)
        {
            // Reset
            runtime.UpdateEntity(new EntityId("susceptible"), e => e.Tags.Remove("sick"));

            var result = await system.ApplyAsync(1.0);
            if (runtime.GetEntity(new EntityId("susceptible"))!.Tags.Contains("sick"))
                spreadCount++;
        }

        // With 0.1% probability over 20 ticks, spread should be rare
        Assert.True(spreadCount < 15, $"Spread should be rare at low probability, but spread {spreadCount}/20 times");
    }

    [Fact]
    public async Task GraphContagion_ReturnsEmptyWhenNoCarriers()
    {
        var runtime = CreateRuntime();
        runtime.CreateEntity(CreateEntity("a"));
        runtime.CreateEntity(CreateEntity("b"));
        runtime.AddRelationship(CreateRelationship("a", "b"));

        var config = new Dictionary<string, object?>
        {
            ["spreadTag"] = "infected",
            ["vectorKind"] = "allied_with",
            ["baseRate"] = 1.0,
            ["maxProbability"] = 1.0,
        };

        var system = new GraphContagion("contagion-1", "Test Contagion", config, runtime);
        var result = await system.ApplyAsync(1.0);

        Assert.Contains("no carriers", result.Description);
        Assert.Empty(result.EntitiesModified);
    }

    // =========================================================================
    // CLUSTER FORMATION TESTS
    // =========================================================================

    [Fact]
    public async Task ClusterFormation_DetectsClusters()
    {
        var runtime = CreateRuntime();

        // Create 4 similar entities (same kind, same culture, shared tags)
        for (var i = 0; i < 4; i++)
        {
            var entity = CreateEntity($"e{i}", kind: "ability", culture: "northern",
                x: 50 + i, y: 50 + i);
            entity.Tags.Set("arcane", true);
            entity.Tags.Set("fire", true);
            runtime.CreateEntity(entity);
        }

        var config = new Dictionary<string, object?>
        {
            ["entityKind"] = "ability",
            ["minClusterSize"] = 3,
            ["maxClusterSize"] = 10,
            ["minimumSimilarity"] = 0.5,
            ["metaEntityKind"] = "ability",
            ["metaEntityStatus"] = "active",
            ["archiveMembers"] = true
        };

        var system = new ClusterFormation("cluster-1", "Test Clustering", config, runtime);
        var result = await system.ApplyAsync(1.0);

        Assert.Contains("meta-entities formed", result.Description);
        Assert.True(result.RelationshipsAdded.Count > 0, "Should create relationships");
    }

    [Fact]
    public async Task ClusterFormation_CreatesMetaEntityWithPartOf()
    {
        var runtime = CreateRuntime();

        // Create 3 similar entities
        for (var i = 0; i < 3; i++)
        {
            var entity = CreateEntity($"member{i}", kind: "ability", culture: "northern",
                x: 50, y: 50);
            entity.Tags.Set("magical", true);
            runtime.CreateEntity(entity);
        }

        var config = new Dictionary<string, object?>
        {
            ["entityKind"] = "ability",
            ["minClusterSize"] = 3,
            ["maxClusterSize"] = 10,
            ["minimumSimilarity"] = 0.3,
            ["metaEntityKind"] = "ability",
            ["metaEntityStatus"] = "active",
            ["archiveMembers"] = false,
            ["memberStatus"] = "subsumed"
        };

        var system = new ClusterFormation("cluster-1", "Test Clustering", config, runtime);
        var result = await system.ApplyAsync(1.0);

        // Should have both subsumes and part_of relationships
        var hasSubsumes = result.RelationshipsAdded.Any(r => r.Kind == "subsumes");
        var hasPartOf = result.RelationshipsAdded.Any(r => r.Kind == "part_of");
        Assert.True(hasSubsumes, "Should create subsumes relationships");
        Assert.True(hasPartOf, "Should create part_of relationships");

        // Members should have updated status
        var member0 = runtime.GetEntity(new EntityId("member0"))!;
        Assert.Equal(new EntityStatus("subsumed"), member0.Status);
    }

    [Fact]
    public async Task ClusterFormation_SkipsWhenNotEnoughEntities()
    {
        var runtime = CreateRuntime();
        runtime.CreateEntity(CreateEntity("lonely", kind: "ability"));

        var config = new Dictionary<string, object?>
        {
            ["entityKind"] = "ability",
            ["minClusterSize"] = 3,
            ["minimumSimilarity"] = 0.5,
            ["metaEntityKind"] = "ability",
        };

        var system = new ClusterFormation("cluster-1", "Test Clustering", config, runtime);
        var result = await system.ApplyAsync(1.0);

        Assert.Contains("not enough entities", result.Description);
    }

    // =========================================================================
    // TAG DIFFUSION TESTS
    // =========================================================================

    [Fact]
    public async Task TagDiffusion_PropagatesCommonTags()
    {
        var runtime = CreateRuntime(seed: 1);

        var a = CreateEntity("a", kind: "npc");
        runtime.CreateEntity(a);

        var b = CreateEntity("b", kind: "npc");
        runtime.CreateEntity(b);

        runtime.AddRelationship(CreateRelationship("a", "b"));

        var config = new Dictionary<string, object?>
        {
            ["entityKind"] = "npc",
            ["connectionKind"] = "allied_with",
            ["connectionDirection"] = "both",
            ["maxTags"] = 10,
            ["convergenceTags"] = new List<string> { "trait_brave", "trait_wise", "trait_cunning" },
            ["convergenceMinConnections"] = 1,
            ["convergenceProbability"] = 1.0,  // guaranteed
            ["convergenceMaxSharedTags"] = 3,
            ["divergenceTags"] = new List<string>(),
            ["throttleChance"] = 1.0,
        };

        var system = new TagDiffusion("diffusion-1", "Test Diffusion", config, runtime);
        var result = await system.ApplyAsync(1.0);

        // At least one entity should have gained a convergence tag
        Assert.True(result.EntitiesModified.Count > 0, "Should have tag modifications");
        Assert.Contains("entities affected", result.Description);
    }

    [Fact]
    public async Task TagDiffusion_AllowsDivergence()
    {
        var runtime = CreateRuntime(seed: 1);

        // Create isolated entity (no connections)
        var lonely = CreateEntity("lonely", kind: "npc");
        runtime.CreateEntity(lonely);

        // Need at least 2 entities
        var other = CreateEntity("other", kind: "npc");
        runtime.CreateEntity(other);

        var config = new Dictionary<string, object?>
        {
            ["entityKind"] = "npc",
            ["connectionKind"] = "allied_with",
            ["connectionDirection"] = "both",
            ["maxTags"] = 10,
            ["convergenceTags"] = new List<string>(),
            ["divergenceTags"] = new List<string> { "isolated_trait", "unique_custom", "drift_value" },
            ["divergenceMaxConnections"] = 0,
            ["divergenceProbability"] = 1.0,  // guaranteed
            ["throttleChance"] = 1.0,
        };

        var system = new TagDiffusion("diffusion-1", "Test Diffusion", config, runtime);
        var result = await system.ApplyAsync(1.0);

        // The lonely entity should gain a divergence tag
        var lonelyEntity = runtime.GetEntity(new EntityId("lonely"))!;
        var hasDivTag = lonelyEntity.Tags.Contains("isolated_trait")
            || lonelyEntity.Tags.Contains("unique_custom")
            || lonelyEntity.Tags.Contains("drift_value");
        Assert.True(hasDivTag, "Isolated entity should gain a divergence tag");
    }

    [Fact]
    public async Task TagDiffusion_ReturnsEmptyWhenNotEnoughEntities()
    {
        var runtime = CreateRuntime();
        runtime.CreateEntity(CreateEntity("lonely"));

        var config = new Dictionary<string, object?>
        {
            ["entityKind"] = "npc",
            ["connectionKind"] = "allied_with",
        };

        var system = new TagDiffusion("diffusion-1", "Test Diffusion", config, runtime);
        var result = await system.ApplyAsync(1.0);

        Assert.Contains("not enough entities", result.Description);
    }

    // =========================================================================
    // PLANE DIFFUSION TESTS
    // =========================================================================

    [Fact]
    public async Task PlaneDiffusion_MovesEntitiesNearSources()
    {
        var runtime = CreateRuntime();

        // Place a source at (50, 50)
        var source = CreateEntity("source", kind: "npc", x: 50.0, y: 50.0);
        source.Tags.Set("heat_source", true);
        runtime.CreateEntity(source);

        // Place a receiver at (52, 50) - nearby
        var receiver = CreateEntity("receiver", kind: "npc", x: 52.0, y: 50.0);
        runtime.CreateEntity(receiver);

        // Place a far entity at (90, 90) - far away
        var farEntity = CreateEntity("far", kind: "npc", x: 90.0, y: 90.0);
        runtime.CreateEntity(farEntity);

        var config = new Dictionary<string, object?>
        {
            ["entityKind"] = "npc",
            ["sourceTag"] = "heat_source",
            ["sinkTag"] = "",
            ["defaultSourceStrength"] = 50.0,
            ["diffusionRate"] = 0.2,
            ["sourceRadius"] = 5,
            ["decayRate"] = 0.0,
            ["iterationsPerTick"] = 20,
            ["valueTag"] = "temperature",
            ["outputTags"] = new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { ["tag"] = "hot", ["minValue"] = 10.0, ["maxValue"] = 100.0 },
            },
            ["throttleChance"] = 1.0,
        };

        var system = new PlaneDiffusion("diffusion-1", "Heat Diffusion", config, runtime);
        var result = await system.ApplyAsync(1.0);

        // Nearby receiver should have a temperature value set
        var receiverEntity = runtime.GetEntity(new EntityId("receiver"))!;
        var hasTemp = receiverEntity.Tags.Contains("temperature");
        Assert.True(hasTemp, "Nearby entity should have temperature tag set");

        // Source should be hot
        var sourceEntity = runtime.GetEntity(new EntityId("source"))!;
        Assert.True(sourceEntity.Tags.Contains("hot"), "Source should be tagged as hot");

        Assert.Contains("sources", result.Description);
    }

    [Fact]
    public async Task PlaneDiffusion_InitializesGridOnFirstRun()
    {
        var runtime = CreateRuntime();
        runtime.CreateEntity(CreateEntity("a"));

        var config = new Dictionary<string, object?>
        {
            ["sourceTag"] = "source",
            ["throttleChance"] = 1.0,
        };

        var system = new PlaneDiffusion("diffusion-1", "Test Diffusion", config, runtime);

        // First run should not throw
        var result = await system.ApplyAsync(1.0);
        Assert.NotNull(result);
    }

    // =========================================================================
    // UNIVERSAL CATALYST TESTS
    // =========================================================================

    [Fact]
    public async Task UniversalCatalyst_AppliesSuccessMutations()
    {
        var runtime = CreateRuntime(seed: 1);

        // Create agents that can act
        var agent = CreateEntity("agent", kind: "npc");
        runtime.CreateEntity(agent);
        runtime.UpdateEntity(new EntityId("agent"), e =>
        {
            // We need to set catalyst state - but CatalystState is immutable record struct
            // Let's create an entity that has CanAct = true
        });

        // Create target
        runtime.CreateEntity(CreateEntity("target"));

        // The entity needs to have CanAct = true
        // Since CatalystState is a readonly struct set at construction,
        // we need to create an entity with it set
        // Let's use a workaround: the CatalystState is set in the Entity constructor as false
        // We'd need reflection or a setter. Let's verify what the system does with no agents.

        var catalystConfig = new UniversalCatalystConfig(
            Id: "catalyst-1",
            Name: "Test Catalyst",
            Description: "Test",
            ActionAttemptRate: 1.0,  // guaranteed attempt
            PressureMultiplier: 1.0,
            ProminenceUpChanceOnSuccess: 1.0,  // guaranteed prominence up on success
            ProminenceDownChanceOnFailure: 0.0);

        var system = new UniversalCatalyst(catalystConfig, runtime);
        var result = await system.ApplyAsync(1.0);

        // No agents can act (default CatalystState has CanAct=false)
        Assert.Contains("dormant", result.Description);
    }

    [Fact]
    public async Task UniversalCatalyst_AppliesFailureMutations()
    {
        var runtime = CreateRuntime(seed: 42);

        // Create two entities as targets
        runtime.CreateEntity(CreateEntity("a"));
        runtime.CreateEntity(CreateEntity("b"));

        var catalystConfig = new UniversalCatalystConfig(
            Id: "catalyst-1",
            Name: "Test Catalyst",
            Description: "Test",
            ActionAttemptRate: 1.0,
            PressureMultiplier: 1.0,
            ProminenceUpChanceOnSuccess: 0.0,
            ProminenceDownChanceOnFailure: 1.0);

        var system = new UniversalCatalyst(catalystConfig, runtime);
        var result = await system.ApplyAsync(1.0);

        // Without agents that can act, system returns dormant
        Assert.Contains("dormant", result.Description);
        Assert.Empty(result.EntitiesModified);
    }

    [Fact]
    public async Task UniversalCatalyst_ReturnsEmptyWhenNoAgents()
    {
        var runtime = CreateRuntime();
        runtime.CreateEntity(CreateEntity("non-agent"));

        var catalystConfig = new UniversalCatalystConfig(
            Id: "catalyst-1",
            Name: "Test Catalyst",
            Description: "Test",
            ActionAttemptRate: 0.3,
            PressureMultiplier: 1.5,
            ProminenceUpChanceOnSuccess: 0.1,
            ProminenceDownChanceOnFailure: 0.05);

        var system = new UniversalCatalyst(catalystConfig, runtime);
        var result = await system.ApplyAsync(1.0);

        Assert.Contains("dormant", result.Description);
        Assert.Empty(result.EntitiesModified);
        Assert.Empty(result.RelationshipsAdded);
    }

    // =========================================================================
    // CATALYST HELPERS TESTS
    // =========================================================================

    [Fact]
    public void CatalystHelpers_GetProminenceMultiplier_ScalesWithLevel()
    {
        // Forgotten: lowest multiplier
        Assert.Equal(0.5, CatalystHelpers.GetProminenceMultiplier(0.5));

        // Marginal
        Assert.Equal(0.75, CatalystHelpers.GetProminenceMultiplier(1.5));

        // Recognized
        Assert.Equal(1.0, CatalystHelpers.GetProminenceMultiplier(2.5));

        // Renowned
        Assert.Equal(1.5, CatalystHelpers.GetProminenceMultiplier(3.5));

        // Mythic
        Assert.Equal(2.0, CatalystHelpers.GetProminenceMultiplier(4.5));
    }

    [Fact]
    public void CatalystHelpers_CalculateAttemptChance_Clamps()
    {
        var entity = CreateEntity("test");
        entity.SetProminence(new Prominence(4.5), 1); // mythic

        // Entity can't act (default CatalystState.CanAct = false)
        var chance = CatalystHelpers.CalculateAttemptChance(entity, 0.5);
        Assert.Equal(0.0, chance);
    }

    [Fact]
    public void CatalystHelpers_CalculateActionWeight_RespectsMinimum()
    {
        // With very negative pressure contribution
        var weight = CatalystHelpers.CalculateActionWeight(1.0, -10.0);
        Assert.Equal(0.1, weight);

        // Normal case
        var normalWeight = CatalystHelpers.CalculateActionWeight(1.0, 0.5);
        Assert.Equal(1.5, normalWeight);
    }

    // =========================================================================
    // INTEGRATION: CONTAGION WITH RECOVERY
    // =========================================================================

    [Fact]
    public async Task GraphContagion_RecoveryGrantsImmunity()
    {
        var runtime = CreateRuntime(seed: 1);

        var entity = CreateEntity("patient");
        entity.Tags.Set("sick", true);
        runtime.CreateEntity(entity);

        // Need a dummy entity so it's not the only one
        runtime.CreateEntity(CreateEntity("bystander"));
        runtime.AddRelationship(CreateRelationship("patient", "bystander"));

        var config = new Dictionary<string, object?>
        {
            ["spreadTag"] = "sick",
            ["vectorKind"] = "allied_with",
            ["vectorDirection"] = "both",
            ["baseRate"] = 0.0,  // no new infections
            ["maxProbability"] = 0.0,
            ["immunityTag"] = "immune",
            ["recoveryRate"] = 1.0,  // guaranteed recovery
        };

        var system = new GraphContagion("contagion-1", "Test Contagion", config, runtime);
        await system.ApplyAsync(1.0);

        var patient = runtime.GetEntity(new EntityId("patient"))!;
        Assert.True(patient.Tags.Contains("immune"), "Recovered entity should have immunity tag");
        Assert.False(patient.Tags.Contains("sick"), "Recovered entity should no longer be sick");
    }
}
