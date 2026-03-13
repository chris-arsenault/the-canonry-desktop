using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Runtime;
using TheCanonry.Engine.Selection;
using TheCanonry.Engine.Statistics;
using TheCanonry.Engine.Systems;
using TheCanonry.Engine.Templates;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;
using ExecutionContext = TheCanonry.Schema.World.ExecutionContext;

namespace TheCanonry.Engine.Tests.Systems;

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

public class GrowthSystemTests
{
    // =========================================================================
    // HELPERS
    // =========================================================================

    private static EngineConfig CreateConfig(
        DistributionTargets? targets = null,
        IReadOnlyList<DeclarativeTemplate>? templates = null,
        double scaleFactor = 1.0,
        int ticksPerEpoch = 10)
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
            Templates: templates ?? [],
            Systems: [],
            Pressures: [],
            TicksPerEpoch: ticksPerEpoch,
            MaxEpochs: 5,
            MaxTicks: 50,
            MaxRelationshipsPerType: 100,
            RelationshipBudget: new RelationshipBudget(20, 50),
            ScaleFactor: scaleFactor,
            DefaultMinDistance: 5.0,
            PressureDeltaSmoothing: 0.5,
            DistributionTargets: targets ?? new DistributionTargets(),
            NameService: new StubNameService(),
            SeedRelationships: [],
            Emitter: new NullEmitter(),
            NarrativeConfig: new NarrativeConfig(true, 0.0));
    }

    private static WorldRuntime CreateRuntime(
        EngineConfig config, GraphStore? graph = null, int seed = 42)
    {
        return new WorldRuntime(config, graph, seed);
    }

    private static Era CreateTestEra(
        string id = "era-1",
        string name = "Test Era",
        Dictionary<string, double>? templateWeights = null)
    {
        return new Era(
            Id: new EraId(id),
            Name: name,
            Summary: "A test era",
            TemplateWeights: templateWeights ?? new Dictionary<string, double>(),
            SystemModifiers: new Dictionary<string, double>(),
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);
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

    private static DistributionTargets CreateTargets(
        params (string Kind, string Subtype, int Target)[] entries)
    {
        var targets = new DistributionTargets
        {
            Entities = []
        };

        foreach (var (kind, subtype, target) in entries)
        {
            if (!targets.Entities.TryGetValue(kind, out var subtypes))
            {
                subtypes = [];
                targets.Entities[kind] = subtypes;
            }
            subtypes[subtype] = target;
        }

        return targets;
    }

    private static DeclarativeTemplate CreateTemplate(
        string id = "tmpl-1",
        string name = "Test Template",
        string creationKind = "npc",
        string creationSubtype = "warrior",
        string selectionKind = "faction",
        int creationCount = 1,
        bool withRelationship = false)
    {
        var creation = new CreationRule(
            EntityRef: "$newEntity",
            Kind: creationKind,
            Subtype: new FixedSubtype(creationSubtype),
            Status: "active",
            Prominence: "marginal",
            Culture: new InheritCulture("$target"),
            Description: new FixedDescription("A test entity"),
            Tags: new Dictionary<string, bool> { ["test"] = true },
            Placement: new PlacementSpec(
                Anchor: new EntityAnchor("$target", false),
                Spacing: new PlacementSpacing(0, []),
                RegionPolicy: new PlacementRegionPolicy(false, false, false),
                Steps: [PlacementStep.Random]),
            Count: new CountRange(creationCount, creationCount),
            CreateChance: 1.0);

        var relationships = new List<RelationshipRule>();
        if (withRelationship)
        {
            relationships.Add(new RelationshipRule(
                Kind: "allied_with",
                Src: "$target",
                Dst: "$newEntity",
                Strength: 0.5,
                Bidirectional: false,
                CatalyzedBy: "",
                Condition: null));
        }

        return new DeclarativeTemplate(
            Id: id,
            Name: name,
            Enabled: true,
            Applicability: [],
            Selection: new SelectionRule(
                Strategy: SelectionStrategy.ByKind,
                Kind: selectionKind,
                Kinds: [],
                Subtypes: [],
                ExcludeSubtypes: [],
                Status: "",
                Statuses: [],
                NotStatus: "",
                RelationshipKind: "",
                MustHave: false,
                Direction: Direction.Both,
                SubtypePreferences: [],
                ReferenceEntity: "",
                MaxDistance: 0,
                MinProminence: "",
                Filters: [],
                SaturationLimits: [],
                PickStrategy: SelectionPickStrategy.Random,
                MaxResults: 10),
            Creation: [creation],
            Relationships: relationships,
            StateUpdates: [],
            Variables: new Dictionary<string, VariableDefinition>(),
            Variants: new TemplateVariants(VariantSelectionMode.FirstMatch, []),
            NarrationTemplate: "");
    }

    // =========================================================================
    // TEST 1: StartEpoch calculates budget from distribution targets
    // =========================================================================

    [Fact]
    public void StartEpoch_CalculatesBudgetFromDistributionTargets()
    {
        var targets = CreateTargets(
            ("faction", "merchant", 10),
            ("faction", "raider", 5),
            ("npc", "warrior", 15));
        var config = CreateConfig(targets: targets);
        var runtime = CreateRuntime(config);
        var growth = new GrowthSystem(config, runtime, new DynamicWeightCalculator());

        var era = CreateTestEra();
        growth.StartEpoch(era);

        // Total target = 10 + 5 + 15 = 30, scaleFactor = 1.0
        Assert.Equal(30, growth.EpochTarget);
    }

    [Fact]
    public void StartEpoch_AppliesScaleFactor()
    {
        var targets = CreateTargets(("faction", "merchant", 10));
        var config = CreateConfig(targets: targets, scaleFactor: 2.5);
        var runtime = CreateRuntime(config);
        var growth = new GrowthSystem(config, runtime, new DynamicWeightCalculator());

        growth.StartEpoch(CreateTestEra());

        // 10 * 2.5 = 25
        Assert.Equal(25, growth.EpochTarget);
    }

    // =========================================================================
    // TEST 2: Template sampling respects era weights
    // =========================================================================

    [Fact]
    public void TemplateSampling_RespectsEraWeights()
    {
        var tmplA = CreateTemplate(id: "tmpl-a", name: "Template A");
        var tmplB = CreateTemplate(id: "tmpl-b", name: "Template B");
        var config = CreateConfig(templates: [tmplA, tmplB]);
        var runtime = CreateRuntime(config, seed: 100);
        var growth = new GrowthSystem(config, runtime, new DynamicWeightCalculator());

        // Give tmpl-a weight 10, tmpl-b weight 0 => always selects tmpl-a
        var era = CreateTestEra(templateWeights: new Dictionary<string, double>
        {
            ["tmpl-a"] = 10.0,
            ["tmpl-b"] = 0.0
        });

        var metrics = new PopulationMetrics();

        // Sample many times to verify tmpl-a is always selected
        for (var i = 0; i < 20; i++)
        {
            var sampled = growth.SampleTemplate(era, [tmplA, tmplB], metrics);
            Assert.NotNull(sampled);
            Assert.Equal("tmpl-a", sampled.Id);
        }
    }

    // =========================================================================
    // TEST 3: Template sampling uses dynamic weight adjustment
    // =========================================================================

    [Fact]
    public void TemplateSampling_UsesDynamicWeightAdjustment()
    {
        // tmpl-a creates faction:merchant (overpopulated), tmpl-b creates npc:warrior (underpopulated)
        var tmplA = CreateTemplate(id: "tmpl-a", creationKind: "faction", creationSubtype: "merchant");
        var tmplB = CreateTemplate(id: "tmpl-b", creationKind: "npc", creationSubtype: "warrior");
        var config = CreateConfig(templates: [tmplA, tmplB]);
        var runtime = CreateRuntime(config, seed: 42);
        var calculator = new DynamicWeightCalculator();
        var growth = new GrowthSystem(config, runtime, calculator);

        // Equal era weights
        var era = CreateTestEra(templateWeights: new Dictionary<string, double>
        {
            ["tmpl-a"] = 1.0,
            ["tmpl-b"] = 1.0
        });

        // faction:merchant is way over target, npc:warrior is way under
        var metrics = new PopulationMetrics();
        metrics.Entities["faction:merchant"] = new EntityMetric
        {
            Kind = "faction", Subtype = "merchant",
            Count = 50, Target = 10,
            Deviation = 4.0 // 400% over target
        };
        metrics.Entities["npc:warrior"] = new EntityMetric
        {
            Kind = "npc", Subtype = "warrior",
            Count = 1, Target = 10,
            Deviation = -0.9 // 90% under target
        };

        // Sample many times — tmpl-b (warrior) should be selected more often
        // because tmpl-a (merchant) gets suppressed by DynamicWeightCalculator
        var counts = new Dictionary<string, int> { ["tmpl-a"] = 0, ["tmpl-b"] = 0 };
        for (var i = 0; i < 100; i++)
        {
            var sampled = growth.SampleTemplate(era, [tmplA, tmplB], metrics);
            if (sampled is not null)
                counts[sampled.Id]++;
        }

        // tmpl-b should be selected significantly more often due to weight adjustment
        Assert.True(counts["tmpl-b"] > counts["tmpl-a"],
            $"Expected tmpl-b ({counts["tmpl-b"]}) > tmpl-a ({counts["tmpl-a"]}) " +
            "due to dynamic weight suppression of overpopulated faction:merchant");
    }

    // =========================================================================
    // TEST 4: Single growth tick creates entities from template
    // =========================================================================

    [Fact]
    public void GrowthTick_CreatesEntitiesFromTemplate()
    {
        var template = CreateTemplate(id: "tmpl-1", selectionKind: "faction");
        var targets = CreateTargets(("npc", "warrior", 10));
        var config = CreateConfig(targets: targets, templates: [template]);

        var graph = new GraphStore();
        var target = CreateTestEntity("target-1", kind: "faction", subtype: "merchant");
        graph.CreateEntity(target);

        var runtime = CreateRuntime(config, graph, seed: 42);
        var growth = new GrowthSystem(config, runtime, new DynamicWeightCalculator());

        var era = CreateTestEra(templateWeights: new Dictionary<string, double>
        {
            ["tmpl-1"] = 1.0
        });

        growth.StartEpoch(era);
        var result = growth.Apply(1.0);

        Assert.Contains("Growth tick: +", result.Description);
    }

    // =========================================================================
    // TEST 5: Growth tick adds created entities to graph
    // =========================================================================

    [Fact]
    public void GrowthTick_AddsCreatedEntitiesToGraph()
    {
        var template = CreateTemplate(id: "tmpl-1", selectionKind: "faction");
        var targets = CreateTargets(("npc", "warrior", 10));
        var config = CreateConfig(targets: targets, templates: [template]);

        var graph = new GraphStore();
        var target = CreateTestEntity("target-1", kind: "faction", subtype: "merchant");
        graph.CreateEntity(target);

        var runtime = CreateRuntime(config, graph, seed: 42);
        var growth = new GrowthSystem(config, runtime, new DynamicWeightCalculator());

        var era = CreateTestEra(templateWeights: new Dictionary<string, double>
        {
            ["tmpl-1"] = 1.0
        });

        var initialCount = runtime.GetEntityCount();
        growth.StartEpoch(era);
        growth.Apply(1.0);

        // At least one new entity should be in the graph
        Assert.True(runtime.GetEntityCount() > initialCount,
            $"Expected entity count to increase from {initialCount}, " +
            $"got {runtime.GetEntityCount()}");
    }

    // =========================================================================
    // TEST 6: Growth tick adds created relationships to graph
    // =========================================================================

    [Fact]
    public void GrowthTick_AddsCreatedRelationshipsToGraph()
    {
        var template = CreateTemplate(
            id: "tmpl-1", selectionKind: "faction", withRelationship: true);
        var targets = CreateTargets(("npc", "warrior", 10));
        var config = CreateConfig(targets: targets, templates: [template]);

        var graph = new GraphStore();
        var target = CreateTestEntity("target-1", kind: "faction", subtype: "merchant");
        graph.CreateEntity(target);

        var runtime = CreateRuntime(config, graph, seed: 42);
        var growth = new GrowthSystem(config, runtime, new DynamicWeightCalculator());

        var era = CreateTestEra(templateWeights: new Dictionary<string, double>
        {
            ["tmpl-1"] = 1.0
        });

        growth.StartEpoch(era);
        growth.Apply(1.0);

        // At least one relationship should be in the graph
        var relationships = runtime.GetRelationships();
        Assert.True(relationships.Count > 0,
            "Expected at least one relationship to be added to the graph");
    }

    // =========================================================================
    // TEST 7: Budget enforcement stops growth when met
    // =========================================================================

    [Fact]
    public void BudgetEnforcement_StopsGrowthWhenTargetMet()
    {
        // Template creates 3 entities per application
        var template = CreateTemplate(
            id: "tmpl-1", selectionKind: "faction", creationCount: 3);
        // Target is only 3 total
        var targets = CreateTargets(("npc", "warrior", 3));
        var config = CreateConfig(targets: targets, templates: [template]);

        var graph = new GraphStore();
        var target = CreateTestEntity("target-1", kind: "faction", subtype: "merchant");
        graph.CreateEntity(target);

        var runtime = CreateRuntime(config, graph, seed: 42);
        var growth = new GrowthSystem(config, runtime, new DynamicWeightCalculator());

        var era = CreateTestEra(templateWeights: new Dictionary<string, double>
        {
            ["tmpl-1"] = 1.0
        });

        growth.StartEpoch(era);
        Assert.Equal(3, growth.EpochTarget);

        // First tick should create entities
        growth.Apply(1.0);

        // Second tick should report target met
        var result = growth.Apply(1.0);
        Assert.Contains("Growth target met", result.Description);
    }

    // =========================================================================
    // TEST 8: CompleteEpoch records yield statistics
    // =========================================================================

    [Fact]
    public void CompleteEpoch_RecordsYieldStatistics()
    {
        var template = CreateTemplate(id: "tmpl-1", selectionKind: "faction");
        var targets = CreateTargets(("npc", "warrior", 10));
        var config = CreateConfig(targets: targets, templates: [template]);

        var graph = new GraphStore();
        var target = CreateTestEntity("target-1", kind: "faction", subtype: "merchant");
        graph.CreateEntity(target);

        var runtime = CreateRuntime(config, graph, seed: 42);
        var growth = new GrowthSystem(config, runtime, new DynamicWeightCalculator());

        var era = CreateTestEra(templateWeights: new Dictionary<string, double>
        {
            ["tmpl-1"] = 1.0
        });

        growth.StartEpoch(era);
        growth.Apply(1.0);

        var summary = growth.CompleteEpoch();

        Assert.Equal(10, summary.Target);
        Assert.True(summary.EntitiesCreated > 0,
            "Expected at least one entity created");
        Assert.True(summary.TemplatesApplied > 0,
            "Expected at least one template applied");
        Assert.Contains("tmpl-1", summary.TemplatesUsed);
    }

    // =========================================================================
    // TEST 9: Empty template list produces no growth
    // =========================================================================

    [Fact]
    public void EmptyTemplateList_ProducesNoGrowth()
    {
        var targets = CreateTargets(("npc", "warrior", 10));
        // No templates configured
        var config = CreateConfig(targets: targets, templates: []);

        var graph = new GraphStore();
        var target = CreateTestEntity("target-1", kind: "faction", subtype: "merchant");
        graph.CreateEntity(target);

        var runtime = CreateRuntime(config, graph, seed: 42);
        var growth = new GrowthSystem(config, runtime, new DynamicWeightCalculator());

        var era = CreateTestEra();

        growth.StartEpoch(era);
        var result = growth.Apply(1.0);

        // No templates means no growth should happen
        var summary = growth.CompleteEpoch();
        Assert.Equal(0, summary.EntitiesCreated);
        Assert.Equal(0, summary.TemplatesApplied);
        Assert.Empty(summary.TemplatesUsed);
    }

    // =========================================================================
    // TEST 10: Growth disabled when modifier is zero
    // =========================================================================

    [Fact]
    public void Apply_ZeroModifier_DisablesGrowth()
    {
        var template = CreateTemplate(id: "tmpl-1", selectionKind: "faction");
        var targets = CreateTargets(("npc", "warrior", 10));
        var config = CreateConfig(targets: targets, templates: [template]);

        var graph = new GraphStore();
        var target = CreateTestEntity("target-1", kind: "faction", subtype: "merchant");
        graph.CreateEntity(target);

        var runtime = CreateRuntime(config, graph, seed: 42);
        var growth = new GrowthSystem(config, runtime, new DynamicWeightCalculator());

        growth.StartEpoch(CreateTestEra(templateWeights: new Dictionary<string, double>
        {
            ["tmpl-1"] = 1.0
        }));

        var result = growth.Apply(0.0);

        Assert.Equal("Growth disabled for this era", result.Description);
        var summary = growth.CompleteEpoch();
        Assert.Equal(0, summary.EntitiesCreated);
    }

    // =========================================================================
    // TEST 11: GetEpochYield returns per-template run counts
    // =========================================================================

    [Fact]
    public void GetEpochYield_ReturnsPerTemplateRunCounts()
    {
        var template = CreateTemplate(id: "tmpl-1", selectionKind: "faction");
        var targets = CreateTargets(("npc", "warrior", 10));
        var config = CreateConfig(targets: targets, templates: [template]);

        var graph = new GraphStore();
        var target = CreateTestEntity("target-1", kind: "faction", subtype: "merchant");
        graph.CreateEntity(target);

        var runtime = CreateRuntime(config, graph, seed: 42);
        var growth = new GrowthSystem(config, runtime, new DynamicWeightCalculator());

        growth.StartEpoch(CreateTestEra(templateWeights: new Dictionary<string, double>
        {
            ["tmpl-1"] = 1.0
        }));

        growth.Apply(1.0);

        var yield = growth.GetEpochYield();
        Assert.True(yield.ContainsKey("tmpl-1"),
            "Expected yield to track tmpl-1");
        Assert.True(yield["tmpl-1"] > 0,
            "Expected tmpl-1 to have been applied at least once");
    }
}
