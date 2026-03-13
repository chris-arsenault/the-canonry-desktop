using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Runtime;
using TheCanonry.Engine.Statistics;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;
using ExecutionContext = TheCanonry.Schema.World.ExecutionContext;

using TemplateInterpreter = TheCanonry.Engine.Templates.TemplateInterpreter;
using DeclarativeTemplate = TheCanonry.Engine.Templates.DeclarativeTemplate;
using CreationRule = TheCanonry.Engine.Templates.CreationRule;
using RelationshipRule = TheCanonry.Engine.Templates.RelationshipRule;
using VariableDefinition = TheCanonry.Engine.Templates.VariableDefinition;
using PlacementSpec = TheCanonry.Engine.Templates.PlacementSpec;
using PlacementStep = TheCanonry.Engine.Templates.PlacementStep;
using FixedSubtype = TheCanonry.Engine.Templates.FixedSubtype;
using FixedCulture = TheCanonry.Engine.Templates.FixedCulture;
using InheritCulture = TheCanonry.Engine.Templates.InheritCulture;
using FixedDescription = TheCanonry.Engine.Templates.FixedDescription;
using EntityAnchor = TheCanonry.Engine.Templates.EntityAnchor;
using PlacementSpacing = TheCanonry.Engine.Templates.PlacementSpacing;
using PlacementRegionPolicy = TheCanonry.Engine.Templates.PlacementRegionPolicy;
using CountRange = TheCanonry.Engine.Templates.CountRange;
using VariantSelectionMode = TheCanonry.Engine.Templates.VariantSelectionMode;
using TemplateVariants = TheCanonry.Engine.Templates.TemplateVariants;

namespace TheCanonry.Engine.Tests.Templates;

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

public class TemplateInterpreterTests
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

    private static WorldRuntime CreateRuntime(GraphStore? graph = null, int? seed = null)
    {
        return new WorldRuntime(CreateMinimalConfig(), graph, seed ?? 42);
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

    private static DeclarativeTemplate CreateEmptyTemplate(string id = "tmpl-empty")
    {
        return new DeclarativeTemplate(
            Id: id,
            Name: "Empty Template",
            Enabled: true,
            Applicability: [],
            Selection: CreateDefaultSelection(),
            Creation: [],
            Relationships: [],
            StateUpdates: [],
            Variables: new Dictionary<string, VariableDefinition>(),
            Variants: new TemplateVariants(VariantSelectionMode.FirstMatch, []),
            NarrationTemplate: "");
    }

    private static SelectionRule CreateDefaultSelection()
    {
        return new SelectionRule(
            Strategy: SelectionStrategy.ByKind,
            Kind: "faction",
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
            MaxResults: 10);
    }

    private static CreationRule CreateSimpleCreationRule(
        string entityRef = "$newEntity",
        string kind = "npc",
        string subtype = "warrior",
        string status = "active",
        string prominence = "marginal",
        string culture = "northern",
        int count = 1)
    {
        return new CreationRule(
            EntityRef: entityRef,
            Kind: kind,
            Subtype: new FixedSubtype(subtype),
            Status: status,
            Prominence: prominence,
            Culture: new InheritCulture("$target"),
            Description: new FixedDescription("A test entity"),
            Tags: new Dictionary<string, bool> { ["test"] = true },
            Placement: new PlacementSpec(
                Anchor: new EntityAnchor("$target", false),
                Spacing: new PlacementSpacing(0, []),
                RegionPolicy: new PlacementRegionPolicy(false, false, false),
                Steps: [PlacementStep.Random]),
            Count: new CountRange(count, count),
            CreateChance: 1.0);
    }

    private static RelationshipRule CreateSimpleRelationshipRule(
        string src = "$target",
        string dst = "$newEntity",
        string kind = "allied_with",
        double strength = 0.5,
        bool bidirectional = false)
    {
        return new RelationshipRule(
            Kind: kind,
            Src: src,
            Dst: dst,
            Strength: strength,
            Bidirectional: bidirectional,
            CatalyzedBy: "",
            Condition: null);
    }

    // =========================================================================
    // TEST 1: Empty template returns empty result
    // =========================================================================

    [Fact]
    public void EmptyTemplate_ReturnsEmptyResult()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        var template = CreateEmptyTemplate();
        var result = TemplateInterpreter.Execute(template, target, runtime);

        Assert.Empty(result.Entities);
        Assert.Empty(result.Relationships);
        Assert.Contains("Empty Template executed", result.Description);
    }

    // =========================================================================
    // TEST 2: Applicability check fails -> empty result
    // =========================================================================

    [Fact]
    public void ApplicabilityFails_ReturnsEmptyResult()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        // Require a pressure that doesn't exist (so condition fails)
        var template = CreateEmptyTemplate() with
        {
            Applicability = [new PressureCondition("conflict", Min: 0.5, Max: 1.0)]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        Assert.Empty(result.Entities);
        Assert.Empty(result.Relationships);
    }

    // =========================================================================
    // TEST 3: Applicability passes when conditions met
    // =========================================================================

    [Fact]
    public void ApplicabilityPasses_WhenConditionsMet()
    {
        var graph = new GraphStore();
        graph.Pressures["conflict"] = 0.7;
        var target = CreateTestEntity("target-1");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        var template = CreateEmptyTemplate() with
        {
            Applicability = [new PressureCondition("conflict", Min: 0.5, Max: 1.0)],
            Creation = [CreateSimpleCreationRule()]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        Assert.Single(result.Entities);
    }

    // =========================================================================
    // TEST 4: Single creation rule creates one entity
    // =========================================================================

    [Fact]
    public void SingleCreationRule_CreatesOneEntity()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        var template = CreateEmptyTemplate() with
        {
            Creation = [CreateSimpleCreationRule()]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        Assert.Single(result.Entities);
        var entity = result.Entities[0];
        Assert.Equal(new EntityKind("npc"), entity.Kind);
        Assert.Equal("warrior", entity.Subtype);
        Assert.Equal("[unnamed]", entity.Name);
        Assert.Equal(new CultureId("northern"), entity.Culture);
        Assert.True(entity.Tags.GetBool("test"));
    }

    // =========================================================================
    // TEST 5: Creation rule with count range creates multiple entities
    // =========================================================================

    [Fact]
    public void CreationRule_WithCountRange_CreatesMultipleEntities()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        var template = CreateEmptyTemplate() with
        {
            Creation = [CreateSimpleCreationRule(count: 3)]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        Assert.Equal(3, result.Entities.Count);
        // All should be unique IDs
        var ids = result.Entities.Select(e => e.Id).ToHashSet();
        Assert.Equal(3, ids.Count);
    }

    // =========================================================================
    // TEST 6: Relationship rule creates relationship between target and created entity
    // =========================================================================

    [Fact]
    public void RelationshipRule_CreatesRelationship()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        var template = CreateEmptyTemplate() with
        {
            Creation = [CreateSimpleCreationRule()],
            Relationships = [CreateSimpleRelationshipRule()]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        Assert.Single(result.Entities);
        Assert.Single(result.Relationships);

        var rel = result.Relationships[0];
        Assert.Equal(target.Id, rel.SourceId);
        Assert.Equal(result.Entities[0].Id, rel.TargetId);
        Assert.Equal(new RelationshipKind("allied_with"), rel.Kind);
        Assert.Equal(0.5, rel.Strength);
    }

    // =========================================================================
    // TEST 7: Bidirectional relationship creates two relationships
    // =========================================================================

    [Fact]
    public void BidirectionalRelationship_CreatesTwoRelationships()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        var template = CreateEmptyTemplate() with
        {
            Creation = [CreateSimpleCreationRule()],
            Relationships = [CreateSimpleRelationshipRule(bidirectional: true)]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        Assert.Equal(2, result.Relationships.Count);
        var forward = result.Relationships[0];
        var reverse = result.Relationships[1];

        Assert.Equal(target.Id, forward.SourceId);
        Assert.Equal(result.Entities[0].Id, forward.TargetId);
        Assert.Equal(result.Entities[0].Id, reverse.SourceId);
        Assert.Equal(target.Id, reverse.TargetId);
    }

    // =========================================================================
    // TEST 8: State updates apply mutations to target
    // =========================================================================

    [Fact]
    public void StateUpdates_ApplyMutationsToTarget()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        var template = CreateEmptyTemplate() with
        {
            StateUpdates = [new SetTagMutation("$target", "upgraded", "true", "")]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        // The mutation should have been applied to the target entity
        Assert.True(target.Tags.GetBool("upgraded"));
    }

    // =========================================================================
    // TEST 9: Variable resolution finds entities from graph
    // =========================================================================

    [Fact]
    public void VariableResolution_FindsEntitiesFromGraph()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1", kind: "faction", subtype: "merchant");
        var enemy = CreateTestEntity("enemy-1", kind: "faction", subtype: "raider");
        graph.CreateEntity(target);
        graph.CreateEntity(enemy);
        var runtime = CreateRuntime(graph);

        var variableSelectRule = new VariableSelectionRule(
            From: new GraphSource(),
            Kind: "faction",
            Kinds: [],
            Subtypes: ["raider"],
            Status: "",
            Statuses: [],
            NotStatus: "",
            Filters: [],
            PreferFilters: [],
            PickStrategy: SelectionPickStrategy.First,
            MaxResults: 1);

        var template = CreateEmptyTemplate() with
        {
            Variables = new Dictionary<string, VariableDefinition>
            {
                ["$enemy"] = new VariableDefinition(variableSelectRule, Required: false)
            },
            Creation = [CreateSimpleCreationRule()]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        // Template should execute and find the enemy variable
        Assert.Single(result.Entities);
        Assert.True(result.ResolvedVariables.ContainsKey("$enemy"));
    }

    // =========================================================================
    // TEST 10: Required variable fails -> empty result
    // =========================================================================

    [Fact]
    public void RequiredVariable_FailsResolution_ReturnsEmptyResult()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        // Require a variable of kind "dragon" which doesn't exist
        var variableSelectRule = new VariableSelectionRule(
            From: new GraphSource(),
            Kind: "dragon",
            Kinds: [],
            Subtypes: [],
            Status: "",
            Statuses: [],
            NotStatus: "",
            Filters: [],
            PreferFilters: [],
            PickStrategy: SelectionPickStrategy.First,
            MaxResults: 1);

        var template = CreateEmptyTemplate() with
        {
            Variables = new Dictionary<string, VariableDefinition>
            {
                ["$dragon"] = new VariableDefinition(variableSelectRule, Required: true)
            },
            Creation = [CreateSimpleCreationRule()]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        // Template should NOT execute because required variable failed
        Assert.Empty(result.Entities);
    }

    // =========================================================================
    // TEST 11: Full template with creation + relationships + state updates
    // =========================================================================

    [Fact]
    public void FullTemplate_CreationRelationshipsAndStateUpdates()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1", kind: "faction", subtype: "merchant");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        var template = CreateEmptyTemplate() with
        {
            Id = "tmpl-full",
            Name = "Full Template",
            Creation =
            [
                CreateSimpleCreationRule(entityRef: "$guard", kind: "npc", subtype: "guard"),
                CreateSimpleCreationRule(entityRef: "$outpost", kind: "location", subtype: "outpost")
            ],
            Relationships =
            [
                CreateSimpleRelationshipRule(src: "$target", dst: "$guard", kind: "employs"),
                CreateSimpleRelationshipRule(src: "$guard", dst: "$outpost", kind: "stationed_at")
            ],
            StateUpdates =
            [
                new SetTagMutation("$target", "has_guards", "true", "")
            ]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        // Verify entities
        Assert.Equal(2, result.Entities.Count);
        Assert.Equal(new EntityKind("npc"), result.Entities[0].Kind);
        Assert.Equal(new EntityKind("location"), result.Entities[1].Kind);

        // Verify relationships
        Assert.Equal(2, result.Relationships.Count);
        Assert.Equal(new RelationshipKind("employs"), result.Relationships[0].Kind);
        Assert.Equal(new RelationshipKind("stationed_at"), result.Relationships[1].Kind);

        // Verify state update
        Assert.True(target.Tags.GetBool("has_guards"));

        // Verify description
        Assert.Equal("Full Template executed", result.Description);

        // Verify entity ref to index mapping
        Assert.Equal(0, result.EntityRefToIndex["$guard"]);
        Assert.Equal(1, result.EntityRefToIndex["$outpost"]);
    }

    // =========================================================================
    // TEST 12: Entity gets correct prominence from label
    // =========================================================================

    [Fact]
    public void CreatedEntity_GetsCorrectProminence()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        var template = CreateEmptyTemplate() with
        {
            Creation = [CreateSimpleCreationRule() with { Prominence = "renowned" }]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        Assert.Single(result.Entities);
        // "renowned" threshold is 3.0, plus 0.5 = 3.5
        Assert.Equal(3.5, result.Entities[0].Prominence.Value);
    }

    // =========================================================================
    // TEST 13: Fixed culture spec uses literal value
    // =========================================================================

    [Fact]
    public void FixedCultureSpec_UsesLiteralValue()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1", culture: "northern");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        var rule = CreateSimpleCreationRule() with
        {
            Culture = new FixedCulture("southern")
        };

        var template = CreateEmptyTemplate() with
        {
            Creation = [rule]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        Assert.Single(result.Entities);
        Assert.Equal(new CultureId("southern"), result.Entities[0].Culture);
    }

    // =========================================================================
    // TEST 14: Relationship condition prevents creation when not met
    // =========================================================================

    [Fact]
    public void RelationshipCondition_PreventsCreation_WhenNotMet()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        // Relationship requires a pressure that doesn't exist
        var relRule = new RelationshipRule(
            Kind: "allied_with",
            Src: "$target",
            Dst: "$newEntity",
            Strength: 0.5,
            Bidirectional: false,
            CatalyzedBy: "",
            Condition: new PressureCondition("nonexistent", Min: 0.5, Max: 1.0));

        var template = CreateEmptyTemplate() with
        {
            Creation = [CreateSimpleCreationRule()],
            Relationships = [relRule]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        // Entity should be created, but relationship should not
        Assert.Single(result.Entities);
        Assert.Empty(result.Relationships);
    }

    // =========================================================================
    // TEST 15: Entity description is populated from FixedDescription
    // =========================================================================

    [Fact]
    public void CreatedEntity_GetsDescription()
    {
        var graph = new GraphStore();
        var target = CreateTestEntity("target-1");
        graph.CreateEntity(target);
        var runtime = CreateRuntime(graph);

        var rule = CreateSimpleCreationRule() with
        {
            Description = new FixedDescription("A mighty warrior")
        };

        var template = CreateEmptyTemplate() with
        {
            Creation = [rule]
        };

        var result = TemplateInterpreter.Execute(template, target, runtime);

        Assert.Single(result.Entities);
        Assert.Equal("A mighty warrior", result.Entities[0].Description);
    }
}
