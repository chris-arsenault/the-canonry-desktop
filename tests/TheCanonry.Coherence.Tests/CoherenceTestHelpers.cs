using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Statistics;
using TheCanonry.Engine.Templates;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

namespace TheCanonry.Coherence.Tests;

// =============================================================================
// TEST STUBS
// =============================================================================

file sealed class StubNameService : INameGenerationService
{
    public Task<string> GenerateAsync(
        EntityKind kind, string subtype, Prominence prominence,
        EntityTags tags, CultureId culture, string? context = null)
        => Task.FromResult("Test Name");
}

file sealed class StubEmitter : ISimulationEmitter
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
// HELPERS
// =============================================================================

/// <summary>
/// Shared helpers for building minimal valid EngineConfig instances.
/// Tests mutate the returned config or schema via with-expressions.
/// </summary>
internal static class CoherenceTestHelpers
{
    /// <summary>
    /// Creates a minimal valid DomainSchema with:
    /// - All framework entity kinds
    /// - One domain entity kind ("faction" with subtypes ["merchant", "military"], statuses ["active", "historical"])
    /// - All framework relationship kinds
    /// - One domain relationship kind ("allied_with")
    /// - One culture ("test")
    /// </summary>
    internal static DomainSchema CreateMinimalSchema()
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
                Subtypes =
                [
                    new SubtypeDefinition { Id = "merchant", Name = "Merchant" },
                    new SubtypeDefinition { Id = "military", Name = "Military" }
                ],
                Statuses =
                [
                    new StatusDefinition { Id = "active", Name = "Active" },
                    new StatusDefinition { Id = "historical", Name = "Historical" }
                ]
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

        return new DomainSchema
        {
            Id = "test-domain",
            Name = "Test Domain",
            Version = "1.0.0",
            EntityKinds = entityKindDefs,
            RelationshipKinds = relKindDefs,
            Cultures =
            [
                new CultureDefinition
                {
                    Id = new CultureId("test"),
                    Name = "Test Culture",
                    Description = "A test culture"
                }
            ]
        };
    }

    /// <summary>
    /// Creates a minimal valid EngineConfig where all elements reference each other correctly:
    /// - One pressure ("pressure-conflict") with a positive mutation source in the template,
    ///   and homeostasis > 0 as a sink
    /// - One template ("tmpl-spawn-faction") with a ModifyPressureMutation(+5) driving the pressure
    /// - One system (ConnectionEvolution, id "sys-evolution") referenced in the era's SystemModifiers
    /// - One era ("era-genesis") referencing the template and system
    /// </summary>
    internal static EngineConfig CreateMinimalConfig()
    {
        var schema = CreateMinimalSchema();

        var pressure = new DeclarativePressure(
            Id: "pressure-conflict",
            Name: "Conflict Pressure",
            Description: "Measures ongoing conflict",
            InitialValue: 0,
            Homeostasis: 0.05,
            Growth: new PressureGrowth(
                PositiveFeedback: [],
                NegativeFeedback: []),
            Contract: new PressureContract([], [], [], new PressureEquilibrium(0, 50, 25, 10)));

        var template = new DeclarativeTemplate(
            Id: "tmpl-spawn-faction",
            Name: "Spawn Faction",
            Enabled: true,
            Applicability: [],
            Selection: new SelectionRule(
                SelectionStrategy.ByKind, "faction", [], [], [], "", [], "",
                "", false, Direction.Source, [], "", 0, "", [], [],
                SelectionPickStrategy.Random, 1),
            Creation:
            [
                new CreationRule(
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
                    CreateChance: 1.0)
            ],
            Relationships:
            [
                new RelationshipRule(
                    Kind: "allied_with",
                    Src: "$newFaction",
                    Dst: "$target",
                    Strength: 0.5,
                    Bidirectional: false,
                    CatalyzedBy: "",
                    Condition: null)
            ],
            StateUpdates:
            [
                new ModifyPressureMutation("pressure-conflict", 5.0)
            ],
            Variables: new Dictionary<string, VariableDefinition>(),
            Variants: new TemplateVariants(VariantSelectionMode.FirstMatch, []),
            NarrationTemplate: "");

        var system = new DeclarativeSystem(
            SystemType: SystemType.ConnectionEvolution,
            Enabled: true,
            Config: new Dictionary<string, object?>
            {
                ["id"] = "sys-evolution",
                ["name"] = "Connection Evolution"
            });

        var era = new Era(
            Id: new EraId("era-genesis"),
            Name: "Genesis",
            Summary: "The beginning era",
            TemplateWeights: new Dictionary<string, double> { ["tmpl-spawn-faction"] = 1.0 },
            SystemModifiers: new Dictionary<string, double> { ["sys-evolution"] = 1.0 },
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
            Systems: [system],
            Pressures: [pressure],
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

    /// <summary>
    /// Creates a bare DeclarativeTemplate with the given id and name, no creation rules
    /// or state updates. Useful when only template identity matters (orphan/weight tests).
    /// </summary>
    internal static DeclarativeTemplate CreateBareTemplate(string id, string name, bool enabled = true)
    {
        return new DeclarativeTemplate(
            Id: id,
            Name: name,
            Enabled: enabled,
            Applicability: [],
            Selection: new SelectionRule(
                SelectionStrategy.ByKind, "faction", [], [], [], "", [], "",
                "", false, Direction.Source, [], "", 0, "", [], [],
                SelectionPickStrategy.Random, 1),
            Creation: [],
            Relationships: [],
            StateUpdates: [],
            Variables: new Dictionary<string, VariableDefinition>(),
            Variants: new TemplateVariants(VariantSelectionMode.FirstMatch, []),
            NarrationTemplate: "");
    }

    /// <summary>
    /// Creates a DeclarativePressure with the specified id, homeostasis, and feedback.
    /// </summary>
    internal static DeclarativePressure CreatePressure(
        string id,
        string name,
        double homeostasis = 0,
        double initialValue = 0,
        IReadOnlyList<Metric>? positiveFeedback = null,
        IReadOnlyList<Metric>? negativeFeedback = null)
    {
        return new DeclarativePressure(
            Id: id,
            Name: name,
            Description: $"Test pressure: {name}",
            InitialValue: initialValue,
            Homeostasis: homeostasis,
            Growth: new PressureGrowth(
                PositiveFeedback: positiveFeedback ?? [],
                NegativeFeedback: negativeFeedback ?? []),
            Contract: new PressureContract([], [], [], new PressureEquilibrium(0, 50, 25, 10)));
    }
}
