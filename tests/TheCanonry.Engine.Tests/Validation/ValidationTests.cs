using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Statistics;
using TheCanonry.Engine.Templates;
using TheCanonry.Engine.Validation;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Tests.Validation;

// =============================================================================
// TEST STUBS
// =============================================================================

file class StubNameService : INameGenerationService
{
    public Task<string> GenerateAsync(
        EntityKind kind, string subtype, Prominence prominence,
        EntityTags tags, CultureId culture, string? context = null)
        => Task.FromResult("Test Name");
}

file class StubEmitter : ISimulationEmitter
{
    public void Progress(ProgressPayload payload) { }
    public void Log(LogLevel level, string message, IReadOnlyDictionary<string, object?>? context = null) { }
    public void EpochStart(EpochStartPayload payload) { }
    public void SystemAction(SystemActionPayload payload) { }
    public void EraTransition(EraId fromEra, string fromName, EraId toEra, string toName, int tick) { }
    public void Error(ErrorPayload payload) { }
    public void Complete() { }
}

// =============================================================================
// TESTS
// =============================================================================

public class ValidationTests
{
    // =========================================================================
    // HELPERS
    // =========================================================================

    /// <summary>
    /// Builds a minimal valid EngineConfig containing all framework primitives,
    /// one era, and one template. Tests mutate the returned config via with-expressions.
    /// </summary>
    private static EngineConfig CreateValidConfig()
    {
        // -- Domain schema with all framework entity kinds and relationship kinds --
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

        // -- One valid template --
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

        // -- One valid era --
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
            TicksPerEpoch: 20,
            MaxEpochs: 10,
            MaxTicks: 200,
            MaxRelationshipsPerType: 50,
            RelationshipBudget: new RelationshipBudget(10, 20),
            ScaleFactor: 1.0,
            DefaultMinDistance: 5.0,
            PressureDeltaSmoothing: 0.1,
            DistributionTargets: new DistributionTargets(),
            NameService: new StubNameService(),
            SeedRelationships: [],
            Emitter: new StubEmitter(),
            NarrativeConfig: new NarrativeConfig(false, 0));
    }

    // =========================================================================
    // ORCHESTRATOR: VALID CONFIG
    // =========================================================================

    [Fact]
    public void ValidConfig_ProducesNoErrors()
    {
        var config = CreateValidConfig();
        var report = ValidationOrchestrator.Validate(config);

        Assert.True(report.IsValid);
        Assert.Empty(report.Errors);
    }

    // =========================================================================
    // FRAMEWORK VALIDATOR: ENTITY KINDS
    // =========================================================================

    [Fact]
    public void MissingFrameworkEntityKind_ProducesError()
    {
        var config = CreateValidConfig();

        // Remove the 'era' entity kind definition
        var filteredEntityKinds = config.Schema.EntityKinds
            .Where(ek => ek.Kind != FrameworkPrimitives.EntityKinds.Era)
            .ToList();

        var brokenSchema = new DomainSchema
        {
            Id = config.Schema.Id,
            Name = config.Schema.Name,
            Version = config.Schema.Version,
            EntityKinds = filteredEntityKinds,
            RelationshipKinds = config.Schema.RelationshipKinds,
            Cultures = config.Schema.Cultures
        };

        var brokenConfig = config with { Schema = brokenSchema };
        var report = ValidationOrchestrator.Validate(brokenConfig);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, e => e.Contains("'era'") && e.Contains("entity kind"));
    }

    // =========================================================================
    // FRAMEWORK VALIDATOR: RELATIONSHIP KINDS
    // =========================================================================

    [Fact]
    public void MissingFrameworkRelationshipKind_ProducesError()
    {
        var config = CreateValidConfig();

        // Remove the 'supersedes' relationship kind definition
        var filteredRelKinds = config.Schema.RelationshipKinds
            .Where(rk => rk.Kind != FrameworkPrimitives.RelationshipKinds.Supersedes)
            .ToList();

        var brokenSchema = new DomainSchema
        {
            Id = config.Schema.Id,
            Name = config.Schema.Name,
            Version = config.Schema.Version,
            EntityKinds = config.Schema.EntityKinds,
            RelationshipKinds = filteredRelKinds,
            Cultures = config.Schema.Cultures
        };

        var brokenConfig = config with { Schema = brokenSchema };
        var report = ValidationOrchestrator.Validate(brokenConfig);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, e => e.Contains("'supersedes'") && e.Contains("relationship kind"));
    }

    // =========================================================================
    // CONTRACT ENFORCER: TEMPLATE ENTITY KINDS
    // =========================================================================

    [Fact]
    public void TemplateReferencingUnknownEntityKind_ProducesError()
    {
        var config = CreateValidConfig();

        // Create a template with a creation rule referencing a non-existent entity kind
        var badTemplate = new DeclarativeTemplate(
            Id: "tmpl-bad-kind",
            Name: "Bad Kind Template",
            Enabled: true,
            Applicability: [],
            Selection: new SelectionRule(
                SelectionStrategy.ByKind, "faction", [], [], [], "", [], "",
                "", false, Direction.Source, [], "", 0, "", [], [],
                SelectionPickStrategy.Random, 1),
            Creation: [new CreationRule(
                EntityRef: "$newEntity",
                Kind: "dragon",  // does not exist in schema
                Subtype: new FixedSubtype("fire"),
                Status: "active",
                Prominence: "recognized",
                Culture: new FixedCulture("test"),
                Description: new FixedDescription("A dragon"),
                Tags: new Dictionary<string, bool>(),
                Placement: new PlacementSpec(
                    new SparseAnchor(false),
                    new PlacementSpacing(5.0, []),
                    new PlacementRegionPolicy(false, false, false),
                    [PlacementStep.Sparse]),
                Count: new CountRange(1, 1),
                CreateChance: 1.0)],
            Relationships: [],
            StateUpdates: [],
            Variables: new Dictionary<string, VariableDefinition>(),
            Variants: new TemplateVariants(VariantSelectionMode.FirstMatch, []),
            NarrationTemplate: "");

        var brokenConfig = config with
        {
            Templates = config.Templates.Append(badTemplate).ToList()
        };
        var report = ValidationOrchestrator.Validate(brokenConfig);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors,
            e => e.Contains("tmpl-bad-kind") && e.Contains("'dragon'"));
    }

    // =========================================================================
    // CONTRACT ENFORCER: ERA TEMPLATE REFERENCES
    // =========================================================================

    [Fact]
    public void EraReferencingUnknownTemplate_ProducesError()
    {
        var config = CreateValidConfig();

        var badEra = new Era(
            Id: new EraId("era-broken"),
            Name: "Broken Era",
            Summary: "An era with bad template reference",
            TemplateWeights: new Dictionary<string, double>
            {
                ["tmpl-does-not-exist"] = 1.0
            },
            SystemModifiers: new Dictionary<string, double>(),
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        var brokenConfig = config with
        {
            Eras = config.Eras.Append(badEra).ToList()
        };
        var report = ValidationOrchestrator.Validate(brokenConfig);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors,
            e => e.Contains("era-broken") && e.Contains("tmpl-does-not-exist"));
    }

    // =========================================================================
    // FRAMEWORK VALIDATOR: EMPTY ERAS
    // =========================================================================

    [Fact]
    public void EmptyErasList_ProducesError()
    {
        var config = CreateValidConfig();
        var brokenConfig = config with { Eras = [] };
        var report = ValidationOrchestrator.Validate(brokenConfig);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, e => e.Contains("era"));
    }

    // =========================================================================
    // FRAMEWORK VALIDATOR: EMPTY TEMPLATES
    // =========================================================================

    [Fact]
    public void EmptyTemplatesList_ProducesError()
    {
        var config = CreateValidConfig();

        // Clear templates AND fix era references so we isolate the template error
        var eraWithNoWeights = new Era(
            Id: new EraId("era-genesis"),
            Name: "Genesis",
            Summary: "The beginning era",
            TemplateWeights: new Dictionary<string, double>(),
            SystemModifiers: new Dictionary<string, double>(),
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        var brokenConfig = config with { Templates = [], Eras = [eraWithNoWeights] };
        var report = ValidationOrchestrator.Validate(brokenConfig);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, e => e.Contains("template"));
    }

    // =========================================================================
    // CONTRACT ENFORCER: TEMPLATE RELATIONSHIP KINDS
    // =========================================================================

    [Fact]
    public void TemplateReferencingUnknownRelationshipKind_ProducesError()
    {
        var config = CreateValidConfig();

        var badTemplate = new DeclarativeTemplate(
            Id: "tmpl-bad-rel",
            Name: "Bad Relationship Template",
            Enabled: true,
            Applicability: [],
            Selection: new SelectionRule(
                SelectionStrategy.ByKind, "faction", [], [], [], "", [], "",
                "", false, Direction.Source, [], "", 0, "", [], [],
                SelectionPickStrategy.Random, 1),
            Creation: [],
            Relationships: [new RelationshipRule(
                Kind: "sworn_enemies",  // does not exist in schema
                Src: "$target",
                Dst: "$newEntity",
                Strength: 0.8,
                Bidirectional: false,
                CatalyzedBy: "",
                Condition: null)],
            StateUpdates: [],
            Variables: new Dictionary<string, VariableDefinition>(),
            Variants: new TemplateVariants(VariantSelectionMode.FirstMatch, []),
            NarrationTemplate: "");

        var brokenConfig = config with
        {
            Templates = config.Templates.Append(badTemplate).ToList()
        };
        var report = ValidationOrchestrator.Validate(brokenConfig);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors,
            e => e.Contains("tmpl-bad-rel") && e.Contains("'sworn_enemies'"));
    }

    // =========================================================================
    // MULTIPLE ERRORS ACCUMULATED
    // =========================================================================

    [Fact]
    public void MultipleViolations_AllReportedInSinglePass()
    {
        var config = CreateValidConfig();

        // Remove framework entity kind AND add bad era reference
        var filteredEntityKinds = config.Schema.EntityKinds
            .Where(ek => ek.Kind != FrameworkPrimitives.EntityKinds.Occurrence)
            .ToList();

        var brokenSchema = new DomainSchema
        {
            Id = config.Schema.Id,
            Name = config.Schema.Name,
            Version = config.Schema.Version,
            EntityKinds = filteredEntityKinds,
            RelationshipKinds = config.Schema.RelationshipKinds,
            Cultures = config.Schema.Cultures
        };

        var badEra = new Era(
            Id: new EraId("era-phantom"),
            Name: "Phantom",
            Summary: "References nonexistent template",
            TemplateWeights: new Dictionary<string, double>
            {
                ["tmpl-phantom"] = 1.0
            },
            SystemModifiers: new Dictionary<string, double>(),
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        var brokenConfig = config with
        {
            Schema = brokenSchema,
            Eras = config.Eras.Append(badEra).ToList()
        };

        var report = ValidationOrchestrator.Validate(brokenConfig);

        Assert.False(report.IsValid);
        // Should have at least 2 errors: missing entity kind + bad era reference
        Assert.True(report.Errors.Count >= 2,
            $"Expected at least 2 errors, got {report.Errors.Count}: {string.Join("; ", report.Errors)}");
        Assert.Contains(report.Errors, e => e.Contains("'occurrence'"));
        Assert.Contains(report.Errors, e => e.Contains("tmpl-phantom"));
    }
}
