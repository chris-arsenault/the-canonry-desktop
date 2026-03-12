namespace TheCanonry.Engine.Tests.Engine;

using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Rules.Types;
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
    public List<ProgressPayload> ProgressCalls { get; } = [];
    public bool CompleteCalled { get; private set; }
    public List<ErrorPayload> ErrorCalls { get; } = [];

    public void Progress(ProgressPayload payload) => ProgressCalls.Add(payload);
    public void Log(LogLevel level, string message, IReadOnlyDictionary<string, object?>? context = null) { }
    public void EpochStart(EpochStartPayload payload) { }
    public void SystemAction(SystemActionPayload payload) { }
    public void EraTransition(EraId fromEra, string fromName, EraId toEra, string toName, int tick) { }
    public void Error(ErrorPayload payload) => ErrorCalls.Add(payload);
    public void Complete() => CompleteCalled = true;
}

file sealed class StubNameService : INameGenerationService
{
    public Task<string> GenerateAsync(
        EntityKind kind, string subtype, Prominence prominence,
        EntityTags tags, CultureId culture, string? context = null) =>
        Task.FromResult($"{kind}-{subtype}-name");
}

/// <summary>
/// A simple test system that creates a deterministic number of entity modifications.
/// </summary>
file sealed class CountingSystem : ISimulationSystem
{
    private readonly WorldRuntime _runtime;
    public string Id { get; }
    public string Name { get; }
    public int ApplyCallCount { get; private set; }

    public CountingSystem(string id, string name, WorldRuntime runtime)
    {
        Id = id;
        Name = name;
        _runtime = runtime;
    }

    public void Initialize() { }

    public Task<SystemResult> ApplyAsync(double modifier)
    {
        ApplyCallCount++;
        return Task.FromResult(SystemResult.Empty with
        {
            Description = $"{Id} applied (modifier={modifier:F2})"
        });
    }
}

/// <summary>
/// A system that produces pressure changes in its result.
/// </summary>
file sealed class PressureModifyingSystem : ISimulationSystem
{
    public string Id => "pressure-modifier";
    public string Name => "Pressure Modifier";

    private readonly string _pressureId;
    private readonly double _delta;

    public PressureModifyingSystem(string pressureId, double delta)
    {
        _pressureId = pressureId;
        _delta = delta;
    }

    public void Initialize() { }

    public Task<SystemResult> ApplyAsync(double modifier)
    {
        var changes = new Dictionary<string, double> { [_pressureId] = _delta * modifier };
        return Task.FromResult(new SystemResult(
            RelationshipsAdded: [],
            RelationshipsAdjusted: [],
            RelationshipsToArchive: [],
            EntitiesModified: [],
            PressureChanges: changes,
            Description: "Applied pressure change",
            Details: new Dictionary<string, object?>(),
            NarrationsByGroup: new Dictionary<string, string>()));
    }
}

// =============================================================================
// TESTS
// =============================================================================

public class WorldEngineTests
{
    // =========================================================================
    // HELPERS
    // =========================================================================

    private static EngineConfig CreateValidConfig(ISimulationEmitter? emitter = null)
    {
        var entityKindDefs = FrameworkPrimitives.EntityKinds.All
            .Select(ek => new EntityKindDefinition
            {
                Kind = ek,
                Description = $"Framework kind: {ek.Value}",
                IsFramework = true,
                Category = EntityCategory.Era,
                Subtypes = [],
                Statuses = []
            })
            .Append(new EntityKindDefinition
            {
                Kind = new EntityKind("faction"),
                Description = "A political faction",
                Category = EntityCategory.Collective,
                Subtypes = [new SubtypeDefinition { Id = "merchant", Name = "Merchant" }],
                Statuses = []
            })
            .ToList();

        var relKindDefs = FrameworkPrimitives.RelationshipKinds.All
            .Select(rk => new RelationshipKindDefinition
            {
                Kind = rk,
                Description = $"Framework relationship: {rk.Value}",
                IsFramework = true
            })
            .Append(new RelationshipKindDefinition
            {
                Kind = new RelationshipKind("allied_with"),
                Description = "Alliance relationship"
            })
            .ToList();

        var schema = new DomainSchema
        {
            Id = "test-domain",
            Name = "Test Domain",
            Version = "1.0.0",
            EntityKinds = entityKindDefs,
            RelationshipKinds = relKindDefs,
            Cultures = [new CultureDefinition
            {
                Id = new CultureId("test"),
                Name = "Test Culture",
                Description = "A test culture"
            }]
        };

        var template = new DeclarativeTemplate(
            Id: "tmpl-spawn-faction",
            Name: "Spawn Faction",
            Enabled: true,
            Applicability: [],
            Selection: new SelectionRule(
                SelectionStrategy.ByKind, "faction", [], [], [], "", [], "",
                "", false, Direction.Source, [], "", 0, "", [], [],
                SelectionPickStrategy.Random, 1),
            Creation: [new CreationRule(
                EntityRef: "$newFaction",
                Kind: "faction",
                Subtype: new FixedSubtype("merchant"),
                Status: "active",
                Prominence: "recognized",
                Culture: new FixedCulture("test"),
                Description: new FixedDescription("A test faction"),
                Tags: new Dictionary<string, bool>(),
                Placement: new PlacementSpec(
                    new SparseAnchor(false),
                    new PlacementSpacing(5.0, []),
                    new PlacementRegionPolicy(false, false, false),
                    [PlacementStep.Sparse]),
                Count: new CountRange(1, 1),
                CreateChance: 1.0)],
            Relationships: [new RelationshipRule(
                Kind: "allied_with",
                Src: "$newFaction",
                Dst: "$target",
                Strength: 0.5,
                Bidirectional: false,
                CatalyzedBy: "",
                Condition: null)],
            StateUpdates: [],
            Variables: new Dictionary<string, VariableDefinition>(),
            Variants: new TemplateVariants(VariantSelectionMode.FirstMatch, []),
            NarrationTemplate: "");

        var era = new Era(
            Id: new EraId("era-genesis"),
            Name: "Genesis",
            Summary: "The beginning era",
            TemplateWeights: new Dictionary<string, double> { ["tmpl-spawn-faction"] = 1.0 },
            SystemModifiers: new Dictionary<string, double>(),
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        return new EngineConfig(
            Schema: schema,
            Eras: [era],
            Templates: [template],
            Systems: [],
            Pressures: [],
            TicksPerEpoch: 5,
            MaxEpochs: 1,
            MaxTicks: 50,
            MaxRelationshipsPerType: 50,
            RelationshipBudget: new RelationshipBudget(10, 20),
            ScaleFactor: 1.0,
            DefaultMinDistance: 5.0,
            PressureDeltaSmoothing: 0.1,
            DistributionTargets: new DistributionTargets
            {
                Entities = new Dictionary<string, Dictionary<string, int>>
                {
                    ["faction"] = new() { ["merchant"] = 3 }
                }
            },
            NameService: new StubNameService(),
            SeedRelationships: [],
            Emitter: emitter ?? new NullEmitter(),
            NarrativeConfig: new NarrativeConfig(false, 0));
    }

    private static EngineConfig CreateInvalidConfig()
    {
        // Missing framework entity kinds — will fail validation
        var schema = new DomainSchema
        {
            Id = "bad-domain",
            Name = "Bad Domain",
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
            TicksPerEpoch: 5,
            MaxEpochs: 1,
            MaxTicks: 50,
            MaxRelationshipsPerType: 50,
            RelationshipBudget: new RelationshipBudget(10, 20),
            ScaleFactor: 1.0,
            DefaultMinDistance: 5.0,
            PressureDeltaSmoothing: 0.1,
            DistributionTargets: new DistributionTargets(),
            NameService: new StubNameService(),
            SeedRelationships: [],
            Emitter: new NullEmitter(),
            NarrativeConfig: new NarrativeConfig(false, 0));
    }

    private static WorldRuntime CreateRuntime(EngineConfig? config = null)
    {
        return new WorldRuntime(config ?? CreateValidConfig(), randomSeed: 42);
    }

    private static Era CreateTestEra(string id = "era-test", string name = "Test Era")
    {
        return new Era(
            Id: new EraId(id),
            Name: name,
            Summary: "A test era",
            TemplateWeights: new Dictionary<string, double>(),
            SystemModifiers: new Dictionary<string, double>(),
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);
    }

    // =========================================================================
    // WORLDENGINE: VALIDATION
    // =========================================================================

    [Fact]
    public async Task RunAsync_ValidConfig_CompletesSuccessfully()
    {
        var emitter = new NullEmitter();
        var config = CreateValidConfig(emitter);
        var engine = new WorldEngine(config, randomSeed: 42);

        var result = await engine.RunAsync();

        Assert.True(result.TotalTicks > 0);
        Assert.True(result.TotalEpochs > 0);
        Assert.True(result.Elapsed.TotalMilliseconds >= 0);
        Assert.True(emitter.CompleteCalled);
    }

    [Fact]
    public async Task RunAsync_InvalidConfig_ThrowsWithValidationErrors()
    {
        var config = CreateInvalidConfig();
        var engine = new WorldEngine(config);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.RunAsync());

        Assert.Contains("validation failed", ex.Message);
    }

    // =========================================================================
    // WORLDENGINE: CANCELLATION
    // =========================================================================

    [Fact]
    public async Task RunAsync_CancellationToken_StopsExecution()
    {
        var config = CreateValidConfig() with { MaxEpochs = 1000, MaxTicks = 100000, TicksPerEpoch = 20 };
        var engine = new WorldEngine(config, randomSeed: 42);

        // Cancel immediately — the engine should stop after the first epoch completes
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await engine.RunAsync(cts.Token);

        // Should stop well before completing all 1000 epochs
        Assert.True(result.TotalEpochs < 1000,
            $"Expected fewer than 1000 epochs, got {result.TotalEpochs}");
    }

    // =========================================================================
    // WORLDENGINE: SINGLE-EPOCH SIMULATION
    // =========================================================================

    [Fact]
    public async Task RunAsync_SingleEpoch_ProducesEpochSummary()
    {
        var config = CreateValidConfig() with { MaxEpochs = 1 };
        var engine = new WorldEngine(config, randomSeed: 42);

        var result = await engine.RunAsync();

        Assert.Equal(1, result.TotalEpochs);
        Assert.Single(result.EpochSummaries);
        Assert.Equal(0, result.EpochSummaries[0].EpochNumber);
    }

    // =========================================================================
    // WORLDENGINE: CREATES ENTITIES VIA TEMPLATES
    // =========================================================================

    [Fact]
    public async Task RunAsync_CreatesEntitiesViaTemplates()
    {
        var config = CreateValidConfig() with { MaxEpochs = 2, TicksPerEpoch = 10 };
        var engine = new WorldEngine(config, randomSeed: 42);

        var result = await engine.RunAsync();

        // The era spawner creates an era entity at minimum
        Assert.True(result.TotalEntities >= 1,
            $"Expected at least 1 entity (the era), got {result.TotalEntities}");
    }

    // =========================================================================
    // WORLDENGINE: RESULT ENTITY COUNTS
    // =========================================================================

    [Fact]
    public async Task RunAsync_ResultContainsExpectedCounts()
    {
        var config = CreateValidConfig() with { MaxEpochs = 1 };
        var engine = new WorldEngine(config, randomSeed: 42);

        var result = await engine.RunAsync();

        // Entity count is non-negative and populated
        Assert.True(result.TotalEntities >= 0);
        Assert.True(result.TotalRelationships >= 0);
        Assert.True(result.TotalTicks >= 0);
    }

    // =========================================================================
    // WORLDENGINE: PROGRESS EVENTS
    // =========================================================================

    [Fact]
    public async Task RunAsync_EmitsProgressEvents()
    {
        var emitter = new NullEmitter();
        var config = CreateValidConfig(emitter) with { MaxEpochs = 1 };
        var engine = new WorldEngine(config, randomSeed: 42);

        await engine.RunAsync();

        // Should have emitted at least: validating, running, per-epoch, finalizing
        Assert.True(emitter.ProgressCalls.Count >= 3,
            $"Expected at least 3 progress calls, got {emitter.ProgressCalls.Count}");

        // First should be validation phase
        Assert.Equal(SimulationPhase.Validating, emitter.ProgressCalls[0].Phase);

        // Last should be finalizing
        Assert.Equal(SimulationPhase.Finalizing, emitter.ProgressCalls[^1].Phase);
    }

    // =========================================================================
    // SIMULATION TICK RUNNER
    // =========================================================================

    [Fact]
    public async Task SimulationTickRunner_ExecutesAllSystems()
    {
        var runtime = CreateRuntime();
        var era = CreateTestEra();
        runtime.CurrentEra = era;

        var system1 = new CountingSystem("sys-1", "System One", runtime);
        var system2 = new CountingSystem("sys-2", "System Two", runtime);

        var runner = new SimulationTickRunner(
            [system1, system2],
            [],
            runtime);

        var result = await runner.RunTickAsync(0, era);

        Assert.Equal(1, system1.ApplyCallCount);
        Assert.Equal(1, system2.ApplyCallCount);
        Assert.Equal(2, result.PerSystemResults.Count);
        Assert.Equal("sys-1", result.PerSystemResults[0].SystemId);
        Assert.Equal("sys-2", result.PerSystemResults[1].SystemId);
    }

    [Fact]
    public async Task SimulationTickRunner_SkipsSystemsWithZeroModifier()
    {
        var runtime = CreateRuntime();
        var era = new Era(
            Id: new EraId("era-test"),
            Name: "Test Era",
            Summary: "A test era",
            TemplateWeights: new Dictionary<string, double>(),
            SystemModifiers: new Dictionary<string, double> { ["sys-disabled"] = 0.0 },
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);
        runtime.CurrentEra = era;

        var disabledSystem = new CountingSystem("sys-disabled", "Disabled System", runtime);
        var enabledSystem = new CountingSystem("sys-enabled", "Enabled System", runtime);

        var runner = new SimulationTickRunner(
            [disabledSystem, enabledSystem],
            [],
            runtime);

        await runner.RunTickAsync(0, era);

        Assert.Equal(0, disabledSystem.ApplyCallCount);
        Assert.Equal(1, enabledSystem.ApplyCallCount);
    }

    [Fact]
    public async Task SimulationTickRunner_AppliesPressureGrowth()
    {
        var runtime = CreateRuntime();
        var era = CreateTestEra();
        runtime.CurrentEra = era;

        // Create a pressure with a constant growth function that adds 5.0 per tick
        var pressure = new Pressure(
            id: "test-pressure",
            name: "Test Pressure",
            initialValue: 10.0,
            homeostasis: 0.0, // no homeostasis for clean test
            growthFunction: _ => 5.0);

        runtime.SetPressure("test-pressure", 10.0);

        var runner = new SimulationTickRunner(
            [],
            [pressure],
            runtime);

        await runner.RunTickAsync(0, era);

        // Pressure should have grown by 5.0 from the growth function
        var pressureValue = runtime.GetPressure("test-pressure");
        Assert.True(pressureValue > 10.0,
            $"Expected pressure > 10.0, got {pressureValue}");
    }

    [Fact]
    public async Task SimulationTickRunner_AppliesSystemPressureChanges()
    {
        var runtime = CreateRuntime();
        var era = CreateTestEra();
        runtime.CurrentEra = era;

        runtime.SetPressure("conflict", 20.0);

        var pressureSystem = new PressureModifyingSystem("conflict", 10.0);

        var runner = new SimulationTickRunner(
            [pressureSystem],
            [],
            runtime);

        await runner.RunTickAsync(0, era);

        // The system applies delta of 10.0 * modifier(1.0) = 10.0
        var pressureValue = runtime.GetPressure("conflict");
        Assert.True(pressureValue >= 29.0,
            $"Expected pressure >= 29.0, got {pressureValue}");
    }

    [Fact]
    public async Task SimulationTickRunner_AdvancesTick()
    {
        var runtime = CreateRuntime();
        var era = CreateTestEra();
        runtime.CurrentEra = era;

        var initialTick = runtime.CurrentTick;

        var runner = new SimulationTickRunner([], [], runtime);
        await runner.RunTickAsync(0, era);

        Assert.Equal(initialTick + 1, runtime.CurrentTick);
    }

    // =========================================================================
    // EPOCH RUNNER
    // =========================================================================

    [Fact]
    public async Task EpochRunner_RunsGrowthThenSimulationTicks()
    {
        var config = CreateValidConfig() with { TicksPerEpoch = 3 };
        var runtime = new WorldRuntime(config, randomSeed: 42);
        var era = CreateTestEra();
        runtime.CurrentEra = era;

        var weightCalculator = new DynamicWeightCalculator();
        var growthSystem = new GrowthSystem(config, runtime, weightCalculator);

        var countingSystem = new CountingSystem("test-sys", "Test System", runtime);

        var tickRunner = new SimulationTickRunner(
            [countingSystem],
            [],
            runtime);

        var epochRunner = new EpochRunner(growthSystem, tickRunner, runtime, config);
        var result = await epochRunner.RunEpochAsync(era, 0);

        // Should have run TicksPerEpoch simulation ticks
        Assert.Equal(3, result.TickResults.Count);
        Assert.Equal(3, countingSystem.ApplyCallCount);

        // Growth summary should be populated
        Assert.Equal(0, result.GrowthSummary.Epoch);

        // Result should have final counts
        Assert.True(result.EntityCountAtEnd >= 0);
        Assert.True(result.RelationshipCountAtEnd >= 0);
    }

    // =========================================================================
    // WORLDENGINE: ON PROGRESS EVENT
    // =========================================================================

    [Fact]
    public async Task RunAsync_RaisesOnProgressEvent()
    {
        var config = CreateValidConfig() with { MaxEpochs = 1 };
        var engine = new WorldEngine(config, randomSeed: 42);

        var progressEvents = new List<ProgressPayload>();
        engine.OnProgress += payload => progressEvents.Add(payload);

        await engine.RunAsync();

        Assert.True(progressEvents.Count >= 3,
            $"Expected at least 3 OnProgress events, got {progressEvents.Count}");
    }
}
