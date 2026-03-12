namespace TheCanonry.Engine.Tests.PressureTests;

using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Runtime;
using TheCanonry.Engine.Statistics;
using TheCanonry.Engine.Systems;
using TheCanonry.Engine.Templates;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

using TheCanonry.Engine.Pressures;

// =============================================================================
// TEST STUBS
// =============================================================================

internal sealed class PressureTestNullEmitter : ISimulationEmitter
{
    public void Progress(ProgressPayload payload) { }
    public void Log(LogLevel level, string message, IReadOnlyDictionary<string, object?>? context = null) { }
    public void EpochStart(EpochStartPayload payload) { }
    public void SystemAction(SystemActionPayload payload) { }
    public void EraTransition(EraId fromEra, string fromName, EraId toEra, string toName, int tick) { }
    public void Error(ErrorPayload payload) { }
    public void Complete() { }
}

internal sealed class PressureTestNameService : INameGenerationService
{
    public Task<string> GenerateAsync(
        EntityKind kind, string subtype, Prominence prominence,
        EntityTags tags, CultureId culture, string? context = null) =>
        Task.FromResult($"{kind}-{subtype}-name");
}

// =============================================================================
// TESTS
// =============================================================================

public class PressureInterpreterTests
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
            NameService: new PressureTestNameService(),
            SeedRelationships: [],
            Emitter: new PressureTestNullEmitter(),
            NarrativeConfig: new NarrativeConfig(true, 0.0));
    }

    private static WorldRuntime CreateRuntime(EngineConfig? config = null, GraphStore? graph = null)
    {
        return new WorldRuntime(config ?? CreateMinimalConfig(), graph, randomSeed: 42);
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

    // =========================================================================
    // PRESSURE CREATION FROM CONFIG
    // =========================================================================

    [Fact]
    public void Create_ProducesCorrectNameAndHomeostasis()
    {
        var runtime = CreateRuntime();

        var config = new DeclarativePressure(
            Id: "conflict",
            Name: "Conflict Pressure",
            Description: "Measures overall conflict level",
            InitialValue: 25.0,
            Homeostasis: 0.05,
            Growth: new PressureGrowth(
                PositiveFeedback: [],
                NegativeFeedback: []),
            Contract: new PressureContract(
                Sources: [],
                Sinks: [],
                Affects: [],
                Equilibrium: new PressureEquilibrium(0, 100, 50, 20)));

        var pressure = PressureInterpreter.Create(config, runtime);

        Assert.Equal("conflict", pressure.Id);
        Assert.Equal("Conflict Pressure", pressure.Name);
        Assert.Equal(25.0, pressure.Value);
        Assert.Equal(0.05, pressure.Homeostasis);
    }

    [Fact]
    public void Create_InitialValueIsClamped()
    {
        var runtime = CreateRuntime();

        var config = new DeclarativePressure(
            Id: "extreme",
            Name: "Extreme",
            Description: "test",
            InitialValue: 200.0,
            Homeostasis: 0.0,
            Growth: new PressureGrowth(PositiveFeedback: [], NegativeFeedback: []),
            Contract: new PressureContract([], [], [], new PressureEquilibrium(0, 100, 50, 20)));

        var pressure = PressureInterpreter.Create(config, runtime);

        Assert.Equal(100.0, pressure.Value);
    }

    // =========================================================================
    // GROWTH FUNCTION EVALUATION
    // =========================================================================

    [Fact]
    public void GrowthFunction_EvaluatesPositiveAndNegativeFeedback()
    {
        var graph = new GraphStore();
        // Add 5 factions and 3 NPCs
        for (var i = 0; i < 5; i++)
            graph.CreateEntity(CreateTestEntity($"f{i}", kind: "faction"));
        for (var i = 0; i < 3; i++)
            graph.CreateEntity(CreateTestEntity($"n{i}", kind: "npc"));

        var runtime = CreateRuntime(graph: graph);

        // Positive: count factions * 0.1 = 5 * 0.1 = 0.5
        // Negative: count NPCs * 0.2 = 3 * 0.2 = 0.6
        // Net: 0.5 - 0.6 = -0.1
        var config = new DeclarativePressure(
            Id: "tension",
            Name: "Tension",
            Description: "test",
            InitialValue: 50.0,
            Homeostasis: 0.0,
            Growth: new PressureGrowth(
                PositiveFeedback:
                [
                    new EntityCountMetric(
                        Kind: "faction",
                        Subtype: "",
                        Status: "",
                        Coefficient: 0.1,
                        Cap: 0)
                ],
                NegativeFeedback:
                [
                    new EntityCountMetric(
                        Kind: "npc",
                        Subtype: "",
                        Status: "",
                        Coefficient: 0.2,
                        Cap: 0)
                ]),
            Contract: new PressureContract([], [], [], new PressureEquilibrium(0, 100, 50, 20)));

        var pressure = PressureInterpreter.Create(config, runtime);
        pressure.ApplyGrowth(runtime);

        // 50.0 + (0.5 - 0.6) = 49.9
        Assert.Equal(49.9, pressure.Value, precision: 5);
    }

    [Fact]
    public void GrowthFunction_WithConstantMetrics()
    {
        var runtime = CreateRuntime();

        // Positive: constant 3.0 * 1.0 = 3.0
        // Negative: constant 1.0 * 1.0 = 1.0
        // Net: 3.0 - 1.0 = 2.0
        var config = new DeclarativePressure(
            Id: "growth",
            Name: "Growth",
            Description: "test",
            InitialValue: 0.0,
            Homeostasis: 0.0,
            Growth: new PressureGrowth(
                PositiveFeedback: [new ConstantMetric(Value: 3.0, Coefficient: 1.0)],
                NegativeFeedback: [new ConstantMetric(Value: 1.0, Coefficient: 1.0)]),
            Contract: new PressureContract([], [], [], new PressureEquilibrium(0, 100, 50, 20)));

        var pressure = PressureInterpreter.Create(config, runtime);
        pressure.ApplyGrowth(runtime);

        Assert.Equal(2.0, pressure.Value, precision: 5);
    }

    // =========================================================================
    // HOMEOSTASIS
    // =========================================================================

    [Fact]
    public void ApplyHomeostasis_PullsTowardZero()
    {
        var runtime = CreateRuntime();

        var pressure = new Pressure(
            id: "stability",
            name: "Stability",
            initialValue: 50.0,
            homeostasis: 0.1,
            growthFunction: _ => 0.0);

        // Homeostasis pull: (0 - 50) * 0.1 * 1.0 = -5.0
        // New value: 50.0 + (-5.0) = 45.0
        pressure.ApplyHomeostasis();

        Assert.Equal(45.0, pressure.Value, precision: 5);
    }

    [Fact]
    public void ApplyHomeostasis_WithRate()
    {
        var pressure = new Pressure(
            id: "stability",
            name: "Stability",
            initialValue: 50.0,
            homeostasis: 0.1,
            growthFunction: _ => 0.0);

        // Homeostasis pull: (0 - 50) * 0.1 * 0.5 = -2.5
        // New value: 50.0 + (-2.5) = 47.5
        pressure.ApplyHomeostasis(rate: 0.5);

        Assert.Equal(47.5, pressure.Value, precision: 5);
    }

    [Fact]
    public void ApplyHomeostasis_NegativeValuePullsUp()
    {
        var pressure = new Pressure(
            id: "stability",
            name: "Stability",
            initialValue: -40.0,
            homeostasis: 0.1,
            growthFunction: _ => 0.0);

        // Homeostasis pull: (0 - (-40)) * 0.1 * 1.0 = 4.0
        // New value: -40.0 + 4.0 = -36.0
        pressure.ApplyHomeostasis();

        Assert.Equal(-36.0, pressure.Value, precision: 5);
    }

    // =========================================================================
    // MIN/MAX CLAMPING
    // =========================================================================

    [Fact]
    public void ApplyGrowth_ClampsToMaxValue()
    {
        var runtime = CreateRuntime();

        var pressure = new Pressure(
            id: "test",
            name: "Test",
            initialValue: 95.0,
            homeostasis: 0.0,
            growthFunction: _ => 20.0); // Would push to 115

        pressure.ApplyGrowth(runtime);

        Assert.Equal(100.0, pressure.Value);
    }

    [Fact]
    public void ApplyGrowth_ClampsToMinValue()
    {
        var runtime = CreateRuntime();

        var pressure = new Pressure(
            id: "test",
            name: "Test",
            initialValue: -95.0,
            homeostasis: 0.0,
            growthFunction: _ => -20.0); // Would push to -115

        pressure.ApplyGrowth(runtime);

        Assert.Equal(-100.0, pressure.Value);
    }

    [Fact]
    public void Pressure_CustomMinMaxBounds()
    {
        var pressure = new Pressure(
            id: "bounded",
            name: "Bounded",
            initialValue: 50.0,
            homeostasis: 0.0,
            growthFunction: _ => 0.0,
            minValue: 0.0,
            maxValue: 50.0);

        // Initial value should be clamped to max
        Assert.Equal(50.0, pressure.Value);

        // Growth pushing beyond max
        pressure.Value = 50.0;
        var runtime = CreateRuntime();
        pressure.ApplyGrowth(runtime);
        Assert.Equal(50.0, pressure.Value);
    }

    // =========================================================================
    // LOAD PRESSURES
    // =========================================================================

    [Fact]
    public void LoadPressures_CreatesAllPressures()
    {
        var runtime = CreateRuntime();
        var emptyGrowth = new PressureGrowth(PositiveFeedback: [], NegativeFeedback: []);
        var emptyContract = new PressureContract([], [], [], new PressureEquilibrium(0, 100, 50, 20));

        var configs = new List<DeclarativePressure>
        {
            new("conflict", "Conflict", "desc", 10.0, 0.05, emptyGrowth, emptyContract),
            new("stability", "Stability", "desc", 60.0, 0.1, emptyGrowth, emptyContract),
            new("growth", "Growth", "desc", 0.0, 0.02, emptyGrowth, emptyContract)
        };

        var pressures = PressureInterpreter.LoadPressures(configs, runtime);

        Assert.Equal(3, pressures.Count);
        Assert.Equal("conflict", pressures[0].Id);
        Assert.Equal(10.0, pressures[0].Value);
        Assert.Equal("stability", pressures[1].Id);
        Assert.Equal(60.0, pressures[1].Value);
        Assert.Equal("growth", pressures[2].Id);
        Assert.Equal(0.0, pressures[2].Value);
    }

    // =========================================================================
    // SYSTEM INTERPRETER
    // =========================================================================

    [Fact]
    public void SystemInterpreter_CreatesEraTransitionFromConfig()
    {
        var runtime = CreateRuntime();

        var config = new DeclarativeSystem(
            SystemType: SystemType.EraTransition,
            Enabled: true,
            Config: new Dictionary<string, object?>
            {
                ["id"] = "era-transition-1",
                ["name"] = "Era Transition",
                ["description"] = "Handles era transitions",
                ["prominenceSnapshotEnabled"] = true,
                ["prominenceSnapshotMinProminence"] = "recognized"
            });

        var system = SystemInterpreter.Create(config, runtime);

        Assert.IsType<Systems.EraTransition>(system);
        Assert.Equal("era-transition-1", system.Id);
        Assert.Equal("Era Transition", system.Name);
    }

    [Fact]
    public void SystemInterpreter_CreatesUniversalCatalystFromConfig()
    {
        var runtime = CreateRuntime();

        var config = new DeclarativeSystem(
            SystemType: SystemType.UniversalCatalyst,
            Enabled: true,
            Config: new Dictionary<string, object?>
            {
                ["id"] = "catalyst-1",
                ["name"] = "Universal Catalyst",
                ["description"] = "Agent actions",
                ["actionAttemptRate"] = 0.3,
                ["pressureMultiplier"] = 1.5,
                ["prominenceUpChanceOnSuccess"] = 0.1,
                ["prominenceDownChanceOnFailure"] = 0.05
            });

        var system = SystemInterpreter.Create(config, runtime);

        Assert.IsType<Systems.UniversalCatalyst>(system);
        Assert.Equal("catalyst-1", system.Id);
        Assert.Equal("Universal Catalyst", system.Name);
    }

    [Fact]
    public void SystemInterpreter_CreatesConnectionEvolutionFromConfig()
    {
        var runtime = CreateRuntime();

        var config = new DeclarativeSystem(
            SystemType: SystemType.ConnectionEvolution,
            Enabled: true,
            Config: new Dictionary<string, object?>
            {
                ["id"] = "conn-evo-1",
                ["name"] = "Connection Evolution"
            });

        var system = SystemInterpreter.Create(config, runtime);

        Assert.IsType<ConnectionEvolution>(system);
        Assert.Equal("conn-evo-1", system.Id);
        Assert.Equal("Connection Evolution", system.Name);
    }

    [Fact]
    public void SystemInterpreter_CreatesRelationshipMaintenanceFromConfig()
    {
        var runtime = CreateRuntime();

        var config = new DeclarativeSystem(
            SystemType: SystemType.RelationshipMaintenance,
            Enabled: true,
            Config: new Dictionary<string, object?>
            {
                ["id"] = "rel-maint-1",
                ["name"] = "Relationship Maintenance",
                ["description"] = "Decay and reinforcement",
                ["maintenanceFrequency"] = 5,
                ["cullThreshold"] = 0.15,
                ["gracePeriod"] = 20,
                ["reinforcementBonus"] = 0.02,
                ["maxStrength"] = 1.0,
                ["proximityRelationshipKinds"] = new List<object?> { "resident_of", "member_of" }
            });

        var system = SystemInterpreter.Create(config, runtime);

        Assert.IsType<Systems.RelationshipMaintenance>(system);
        Assert.Equal("rel-maint-1", system.Id);
        Assert.Equal("Relationship Maintenance", system.Name);
    }

    [Fact]
    public void SystemInterpreter_LoadSystems_FiltersDisabledSystems()
    {
        var runtime = CreateRuntime();

        var configs = new List<DeclarativeSystem>
        {
            new(SystemType.ConnectionEvolution, true, new Dictionary<string, object?>
            {
                ["id"] = "enabled-1",
                ["name"] = "Enabled System"
            }),
            new(SystemType.GraphContagion, false, new Dictionary<string, object?>
            {
                ["id"] = "disabled-1",
                ["name"] = "Disabled System"
            }),
            new(SystemType.ThresholdTrigger, true, new Dictionary<string, object?>
            {
                ["id"] = "enabled-2",
                ["name"] = "Another Enabled"
            })
        };

        var systems = SystemInterpreter.LoadSystems(configs, runtime);

        Assert.Equal(2, systems.Count);
        Assert.Equal("enabled-1", systems[0].Id);
        Assert.Equal("enabled-2", systems[1].Id);
    }

    [Fact]
    public void SystemInterpreter_LoadSystems_SkipsGrowthAndEraSpawner()
    {
        var runtime = CreateRuntime();

        var configs = new List<DeclarativeSystem>
        {
            new(SystemType.EraSpawner, true, new Dictionary<string, object?>
            {
                ["id"] = "spawner",
                ["name"] = "Era Spawner"
            }),
            new(SystemType.Growth, true, new Dictionary<string, object?>
            {
                ["id"] = "growth",
                ["name"] = "Growth"
            }),
            new(SystemType.TagDiffusion, true, new Dictionary<string, object?>
            {
                ["id"] = "tag-diff",
                ["name"] = "Tag Diffusion"
            })
        };

        var systems = SystemInterpreter.LoadSystems(configs, runtime);

        Assert.Single(systems);
        Assert.Equal("tag-diff", systems[0].Id);
    }

    [Fact]
    public async Task PlaceholderSystems_ReturnEmptyResult()
    {
        var runtime = CreateRuntime();

        var systemConfigs = new (SystemType Type, string Id)[]
        {
            (SystemType.ConnectionEvolution, "ce"),
            (SystemType.GraphContagion, "gc"),
            (SystemType.ThresholdTrigger, "tt"),
            (SystemType.ClusterFormation, "cf"),
            (SystemType.TagDiffusion, "td"),
            (SystemType.PlaneDiffusion, "pd"),
        };

        foreach (var (type, id) in systemConfigs)
        {
            var config = new DeclarativeSystem(
                type, true,
                new Dictionary<string, object?> { ["id"] = id, ["name"] = $"Test {id}" });

            var system = SystemInterpreter.Create(config, runtime);
            system.Initialize(); // Should not throw

            var result = await system.ApplyAsync(1.0);
            Assert.Same(SystemResult.Empty, result);
        }
    }
}
