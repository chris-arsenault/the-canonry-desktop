namespace TheCanonry.Engine.Tests.Config;

using TheCanonry.Engine.Config;
using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Templates;

public class EngineConfigLoaderTests
{
    // Domain directory path — resolved relative to test execution directory.
    // The domain files live at the repo root: domain/default-project/
    private static readonly string DomainDir = ResolveDomainDir();

    private static string ResolveDomainDir()
    {
        // Walk up from the test bin directory to find the repo root
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "domain", "default-project");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        return Path.Combine(AppContext.BaseDirectory, "domain", "default-project");
    }

    private static bool DomainFilesExist =>
        Directory.Exists(DomainDir) && File.Exists(Path.Combine(DomainDir, "eras.json"));

    // =========================================================================
    // ERAS
    // =========================================================================

    [Fact]
    public void LoadEras_ReturnsCorrectCount()
    {

        var eras = EngineConfigLoader.LoadEras(DomainDir);

        Assert.Equal(5, eras.Count);
    }

    [Fact]
    public void LoadEras_FirstEraHasCorrectName()
    {

        var eras = EngineConfigLoader.LoadEras(DomainDir);

        Assert.Equal("The Great Thaw", eras[0].Name);
        Assert.Equal("the-great-thaw", eras[0].Id.Value);
    }

    [Fact]
    public void LoadEras_HasTemplateWeights()
    {

        var eras = EngineConfigLoader.LoadEras(DomainDir);

        Assert.True(eras[0].TemplateWeights.Count > 0);
        Assert.True(eras[0].TemplateWeights.ContainsKey("colony_founding"));
    }

    [Fact]
    public void LoadEras_HasSystemModifiers()
    {

        var eras = EngineConfigLoader.LoadEras(DomainDir);

        Assert.True(eras[0].SystemModifiers.Count > 0);
    }

    [Fact]
    public void LoadEras_HasExitConditions()
    {

        var eras = EngineConfigLoader.LoadEras(DomainDir);

        Assert.True(eras[0].ExitConditions.Count > 0);
        Assert.IsType<GrowthPhasesCompleteCondition>(eras[0].ExitConditions[0]);
    }

    [Fact]
    public void LoadEras_HasEntryEffects()
    {

        var eras = EngineConfigLoader.LoadEras(DomainDir);

        Assert.True(eras[0].EntryEffects.Mutations.Count > 0);
        Assert.Equal("magical_stability", eras[0].EntryEffects.Mutations[0].PressureId);
    }

    [Fact]
    public void LoadEras_FactionWarsHasPressureExitCondition()
    {

        var eras = EngineConfigLoader.LoadEras(DomainDir);
        var factionWars = eras[1];

        Assert.Equal("The Faction Wars", factionWars.Name);
        // Has pressure exit condition
        var pressureCond = factionWars.ExitConditions.OfType<PressureCondition>().FirstOrDefault();
        Assert.NotNull(pressureCond);
    }

    // =========================================================================
    // TEMPLATES (generators.json)
    // =========================================================================

    [Fact]
    public void LoadTemplates_ReturnsNonEmpty()
    {

        var templates = EngineConfigLoader.LoadTemplates(DomainDir);

        Assert.True(templates.Count > 0);
    }

    [Fact]
    public void LoadTemplates_FirstTemplateHasCreationRules()
    {

        var templates = EngineConfigLoader.LoadTemplates(DomainDir);
        var first = templates[0];

        Assert.NotEmpty(first.Id);
        Assert.NotEmpty(first.Name);
        Assert.True(first.Creation.Count > 0);
    }

    [Fact]
    public void LoadTemplates_HeroEmergenceHasCorrectStructure()
    {

        var templates = EngineConfigLoader.LoadTemplates(DomainDir);
        var hero = templates.First(t => t.Id == "hero_emergence");

        // Has applicability conditions
        Assert.True(hero.Applicability.Count > 0);
        Assert.IsType<OrCondition>(hero.Applicability[0]);

        // Has selection rule
        Assert.Equal(SelectionStrategy.ByKind, hero.Selection.Strategy);
        Assert.Equal("location", hero.Selection.Kind);

        // Has creation rules with proper subtypes
        Assert.True(hero.Creation.Count >= 2);
        var heroCreation = hero.Creation.First(c => c.EntityRef == "$hero");
        Assert.IsType<FixedSubtype>(heroCreation.Subtype);
        Assert.Equal("hero", ((FixedSubtype)heroCreation.Subtype).Value);

        // Has relationships
        Assert.True(hero.Relationships.Count > 0);

        // Has state updates (mutations)
        Assert.True(hero.StateUpdates.Count > 0);

        // Has variables
        Assert.True(hero.Variables.Count > 0);

        // Has narration template
        Assert.NotEmpty(hero.NarrationTemplate);
    }

    [Fact]
    public void LoadTemplates_ConditionalSubtypeDeserializes()
    {

        var templates = EngineConfigLoader.LoadTemplates(DomainDir);
        var splinterTemplate = templates.First(t => t.Id == "faction_splinter");

        var splinterCreation = splinterTemplate.Creation.First(c => c.EntityRef == "$splinter");
        Assert.IsType<ConditionalSubtype>(splinterCreation.Subtype);

        var conditional = (ConditionalSubtype)splinterCreation.Subtype;
        Assert.NotEmpty(conditional.When);
        Assert.IsType<TargetSubtypeCondition>(conditional.When[0].Condition);
    }

    [Fact]
    public void LoadTemplates_CultureSpecDeserializes()
    {

        var templates = EngineConfigLoader.LoadTemplates(DomainDir);
        var hero = templates.First(t => t.Id == "hero_emergence");
        var heroCreation = hero.Creation.First(c => c.EntityRef == "$hero");

        Assert.IsType<InheritCulture>(heroCreation.Culture);
        Assert.Equal("$target", ((InheritCulture)heroCreation.Culture).Ref);
    }

    [Fact]
    public void LoadTemplates_FixedCultureDeserializes()
    {

        var templates = EngineConfigLoader.LoadTemplates(DomainDir);
        var orcaTemplate = templates.First(t => t.Id == "orca_raider_arrival");
        var orcaCreation = orcaTemplate.Creation.First(c => c.EntityRef == "$orca1");

        Assert.IsType<FixedCulture>(orcaCreation.Culture);
        Assert.Equal("orca", ((FixedCulture)orcaCreation.Culture).Id);
    }

    [Fact]
    public void LoadTemplates_PlacementAnchorDeserializes()
    {

        var templates = EngineConfigLoader.LoadTemplates(DomainDir);
        var hero = templates.First(t => t.Id == "hero_emergence");

        // Hero's placement uses a culture anchor
        var heroCreation = hero.Creation.First(c => c.EntityRef == "$hero");
        Assert.IsType<CultureAnchor>(heroCreation.Placement.Anchor);

        // Hero weapon uses sparse anchor
        var weaponCreation = hero.Creation.FirstOrDefault(c => c.EntityRef == "$heroWeapon");
        if (weaponCreation != null)
        {
            Assert.IsType<SparseAnchor>(weaponCreation.Placement.Anchor);
        }
    }

    [Fact]
    public void LoadTemplates_EntityAnchorDeserializes()
    {

        var templates = EngineConfigLoader.LoadTemplates(DomainDir);
        var splinter = templates.First(t => t.Id == "faction_splinter");
        var splinterCreation = splinter.Creation.First(c => c.EntityRef == "$splinter");

        Assert.IsType<EntityAnchor>(splinterCreation.Placement.Anchor);
        Assert.Equal("$target", ((EntityAnchor)splinterCreation.Placement.Anchor).Ref);
    }

    [Fact]
    public void LoadTemplates_VariantsDeserialize()
    {

        var templates = EngineConfigLoader.LoadTemplates(DomainDir);
        var hero = templates.First(t => t.Id == "hero_emergence");

        Assert.Equal(VariantSelectionMode.FirstMatch, hero.Variants.Selection);
        Assert.True(hero.Variants.Options.Count > 0);
        Assert.Equal("Armed Hero", hero.Variants.Options[0].Name);
        Assert.IsType<RandomChanceCondition>(hero.Variants.Options[0].When);
    }

    [Fact]
    public void LoadTemplates_RelationshipConditionsDeserialize()
    {

        var templates = EngineConfigLoader.LoadTemplates(DomainDir);
        var hero = templates.First(t => t.Id == "hero_emergence");

        // Some relationships have conditions
        var conditionalRel = hero.Relationships.FirstOrDefault(r => r.Condition != null);
        Assert.NotNull(conditionalRel);
        Assert.IsType<EntityExistsCondition>(conditionalRel.Condition);
    }

    [Fact]
    public void LoadTemplates_DescriptionSpecDeserializes()
    {

        var templates = EngineConfigLoader.LoadTemplates(DomainDir);
        var hero = templates.First(t => t.Id == "hero_emergence");
        var heroCreation = hero.Creation.First(c => c.EntityRef == "$hero");

        Assert.IsType<TemplateDescription>(heroCreation.Description);
        var desc = (TemplateDescription)heroCreation.Description;
        Assert.Contains("colonyName", desc.Replacements.Keys);
    }

    [Fact]
    public void LoadTemplates_CreateChanceDeserializes()
    {

        var templates = EngineConfigLoader.LoadTemplates(DomainDir);
        var hero = templates.First(t => t.Id == "hero_emergence");
        var weapon = hero.Creation.FirstOrDefault(c => c.EntityRef == "$heroWeapon");

        if (weapon != null)
        {
            Assert.Equal(0.25, weapon.CreateChance);
        }
    }

    // =========================================================================
    // SYSTEMS
    // =========================================================================

    [Fact]
    public void LoadSystems_ReturnsNonEmpty()
    {

        var systems = EngineConfigLoader.LoadSystems(DomainDir);

        Assert.True(systems.Count > 0);
    }

    [Fact]
    public void LoadSystems_ContainsFrameworkSystems()
    {

        var systems = EngineConfigLoader.LoadSystems(DomainDir);

        Assert.Contains(systems, s => s.SystemType == SystemType.EraSpawner);
        Assert.Contains(systems, s => s.SystemType == SystemType.EraTransition);
        Assert.Contains(systems, s => s.SystemType == SystemType.UniversalCatalyst);
        Assert.Contains(systems, s => s.SystemType == SystemType.RelationshipMaintenance);
    }

    [Fact]
    public void LoadSystems_ContainsDomainSystems()
    {

        var systems = EngineConfigLoader.LoadSystems(DomainDir);

        Assert.Contains(systems, s => s.SystemType == SystemType.ConnectionEvolution);
        Assert.Contains(systems, s => s.SystemType == SystemType.ThresholdTrigger);
        Assert.Contains(systems, s => s.SystemType == SystemType.ClusterFormation);
        Assert.Contains(systems, s => s.SystemType == SystemType.GraphContagion);
        Assert.Contains(systems, s => s.SystemType == SystemType.TagDiffusion);
        Assert.Contains(systems, s => s.SystemType == SystemType.PlaneDiffusion);
    }

    [Fact]
    public void LoadSystems_ConfigContainsExpectedKeys()
    {

        var systems = EngineConfigLoader.LoadSystems(DomainDir);
        var eraSpawner = systems.First(s => s.SystemType == SystemType.EraSpawner);

        Assert.True(eraSpawner.Config.ContainsKey("id"));
        Assert.True(eraSpawner.Config.ContainsKey("name"));
        Assert.Equal("era_spawner", eraSpawner.Config["id"]);
    }

    [Fact]
    public void LoadSystems_AllSystemsAreEnabled()
    {

        var systems = EngineConfigLoader.LoadSystems(DomainDir);

        Assert.All(systems, s => Assert.True(s.Enabled));
    }

    // =========================================================================
    // PRESSURES
    // =========================================================================

    [Fact]
    public void LoadPressures_ReturnsCorrectCount()
    {

        var pressures = EngineConfigLoader.LoadPressures(DomainDir);

        Assert.Equal(4, pressures.Count);
    }

    [Fact]
    public void LoadPressures_FirstPressureHasCorrectName()
    {

        var pressures = EngineConfigLoader.LoadPressures(DomainDir);

        Assert.Equal("Resource Availibility", pressures[0].Name);
        Assert.Equal("resource_availibility", pressures[0].Id);
    }

    [Fact]
    public void LoadPressures_HasGrowthFeedback()
    {

        var pressures = EngineConfigLoader.LoadPressures(DomainDir);
        var resource = pressures[0];

        Assert.True(resource.Growth.PositiveFeedback.Count > 0);
        Assert.True(resource.Growth.NegativeFeedback.Count > 0);
    }

    [Fact]
    public void LoadPressures_EntityCountMetricDeserializes()
    {

        var pressures = EngineConfigLoader.LoadPressures(DomainDir);
        var resource = pressures[0];

        var entityCountMetric = resource.Growth.PositiveFeedback
            .OfType<EntityCountMetric>().FirstOrDefault();
        Assert.NotNull(entityCountMetric);
        Assert.Equal("location", entityCountMetric.Kind);
        Assert.Equal("resource_node", entityCountMetric.Subtype);
    }

    [Fact]
    public void LoadPressures_RatioMetricDeserializes()
    {

        var pressures = EngineConfigLoader.LoadPressures(DomainDir);
        var resource = pressures[0];

        var ratioMetric = resource.Growth.NegativeFeedback
            .OfType<RatioMetric>().FirstOrDefault();
        Assert.NotNull(ratioMetric);
        Assert.IsType<SimpleEntityCount>(ratioMetric.Numerator);
        Assert.IsType<SimpleTotalEntities>(ratioMetric.Denominator);
    }

    [Fact]
    public void LoadPressures_RelationshipCountMetricDeserializes()
    {

        var pressures = EngineConfigLoader.LoadPressures(DomainDir);
        var security = pressures.First(p => p.Id == "iceberg_security");

        var relCountMetric = security.Growth.PositiveFeedback
            .OfType<RelationshipCountMetric>().FirstOrDefault();
        Assert.NotNull(relCountMetric);
        Assert.Contains("allied_with", relCountMetric.RelationshipKinds);
    }

    [Fact]
    public void LoadPressures_TagCountMetricDeserializes()
    {

        var pressures = EngineConfigLoader.LoadPressures(DomainDir);
        var security = pressures.First(p => p.Id == "iceberg_security");

        var tagCountMetric = security.Growth.NegativeFeedback
            .OfType<TagCountMetric>().FirstOrDefault();
        Assert.NotNull(tagCountMetric);
        Assert.True(tagCountMetric.Tags.Count > 0);
    }

    [Fact]
    public void LoadPressures_HasInitialValueAndHomeostasis()
    {

        var pressures = EngineConfigLoader.LoadPressures(DomainDir);
        var resource = pressures[0];

        Assert.Equal(15, resource.InitialValue);
        Assert.Equal(0.1, resource.Homeostasis);
    }
}
