using TheCanonry.Coherence.Types;
using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Templates;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Coherence.Tests;

/// <summary>
/// Tests for the top-level CoherenceValidator.Validate() orchestration.
/// Verifies the report aggregation, status, and interaction between rules.
/// </summary>
public class CoherenceValidatorTests
{
    // =========================================================================
    // CLEAN CONFIG
    // =========================================================================

    [Fact]
    public void ValidConfig_ReturnsCleanReport()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var report = CoherenceValidator.Validate(config);

        Assert.True(report.IsClean);
        Assert.Empty(report.Issues);
        Assert.Empty(report.Errors);
        Assert.Empty(report.Warnings);
        Assert.Equal(CoherenceStatus.Clean, report.Status);
    }

    // =========================================================================
    // REPORT STATUS
    // =========================================================================

    [Fact]
    public void ConfigWithWarnings_ReturnsWarningStatus()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // Create an orphan template to produce a warning
        var orphan = CoherenceTestHelpers.CreateBareTemplate("tmpl-orphan", "Orphan");
        var updated = config with
        {
            Templates = config.Templates.Append(orphan).ToList()
        };

        var report = CoherenceValidator.Validate(updated);

        Assert.False(report.IsClean);
        Assert.Equal(CoherenceStatus.Warning, report.Status);
        Assert.NotEmpty(report.Warnings);
        Assert.Empty(report.Errors);
    }

    // =========================================================================
    // MULTIPLE RULE ISSUES COLLECTED
    // =========================================================================

    [Fact]
    public void MultipleRulesTrigger_AllCollectedInReport()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // 1. Add an orphan template -> orphan-generators
        var orphan = CoherenceTestHelpers.CreateBareTemplate("tmpl-orphan", "Orphan");

        // 2. Add a pressure with no sinks -> pressure-without-sinks
        var noSinkPressure = CoherenceTestHelpers.CreatePressure(
            "pressure-growth", "Growth Pressure",
            homeostasis: 0);

        // 3. Add the pressure source in a template so it doesn't also trigger pressure-without-sources
        var templateWithSource = config.Templates[0] with
        {
            StateUpdates =
            [
                new ModifyPressureMutation("pressure-conflict", 5.0),
                new ModifyPressureMutation("pressure-growth", 3.0)
            ]
        };

        var updated = config with
        {
            Templates = [templateWithSource, orphan],
            Pressures = config.Pressures.Append(noSinkPressure).ToList()
        };

        var report = CoherenceValidator.Validate(updated);

        Assert.False(report.IsClean);
        Assert.Contains(report.Issues, i => i.RuleId == "orphan-generators");
        Assert.Contains(report.Issues, i => i.RuleId == "pressure-without-sinks");
        Assert.True(report.Issues.Count >= 2);
    }

    // =========================================================================
    // ISSUES CONTAIN CORRECT RULE IDS
    // =========================================================================

    [Fact]
    public void EachRuleProducesCorrectRuleId()
    {
        // Build a config with problems for multiple rules
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // orphan-generators
        var orphan = CoherenceTestHelpers.CreateBareTemplate("tmpl-orphan", "Orphan");

        // pressure-without-sources: add a pressure with no sources
        var noSourcePressure = CoherenceTestHelpers.CreatePressure(
            "pressure-isolation", "Isolation Pressure", homeostasis: 0.05);

        // invalid-subtype-references
        var badCreation = config.Templates[0].Creation[0] with
        {
            Subtype = new FixedSubtype("pirate")
        };
        var templateWithBadSubtype = config.Templates[0] with { Creation = [badCreation] };

        var updated = config with
        {
            Templates = [templateWithBadSubtype, orphan],
            Pressures = config.Pressures.Append(noSourcePressure).ToList()
        };

        var report = CoherenceValidator.Validate(updated);

        var ruleIds = report.Issues.Select(i => i.RuleId).ToHashSet();
        Assert.Contains("orphan-generators", ruleIds);
        Assert.Contains("pressure-without-sources", ruleIds);
        Assert.Contains("invalid-subtype-references", ruleIds);
    }

    // =========================================================================
    // ISSUES LIST FILTERING
    // =========================================================================

    [Fact]
    public void WarningsFilter_OnlyReturnsWarnings()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var orphan = CoherenceTestHelpers.CreateBareTemplate("tmpl-orphan", "Orphan");
        var updated = config with
        {
            Templates = config.Templates.Append(orphan).ToList()
        };

        var report = CoherenceValidator.Validate(updated);

        Assert.All(report.Warnings, w => Assert.Equal(CoherenceIssueSeverity.Warning, w.Severity));
    }

    // =========================================================================
    // EMPTY CONFIG EDGE CASES
    // =========================================================================

    [Fact]
    public void ConfigWithNoPressures_NoTemplates_NoSystems_ProducesNoIssues()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // Strip down to bare minimum: no templates, systems, pressures, and era with empty weights
        var bareEra = new Era(
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

        var updated = config with
        {
            Templates = [],
            Systems = [],
            Pressures = [],
            Eras = [bareEra]
        };

        var report = CoherenceValidator.Validate(updated);
        Assert.True(report.IsClean);
    }

    // =========================================================================
    // ALL COHERENCE RULES ARE WARNING SEVERITY
    // =========================================================================

    [Fact]
    public void AllCoherenceIssues_AreWarningSeverity()
    {
        // Coherence issues are warnings (as opposed to engine validation errors).
        // Build a config that triggers as many rules as possible and verify all are warnings.
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // orphan template
        var orphan = CoherenceTestHelpers.CreateBareTemplate("tmpl-orphan", "Orphan");

        // orphan system
        var orphanSys = new DeclarativeSystem(
            SystemType: SystemType.GraphContagion,
            Enabled: true,
            Config: new Dictionary<string, object?> { ["id"] = "sys-orphan", ["name"] = "Orphan" });

        // pressure without sinks
        var noSink = CoherenceTestHelpers.CreatePressure("p-nosink", "No Sink", homeostasis: 0);

        // pressure without sources (no template drives it)
        var noSource = CoherenceTestHelpers.CreatePressure("p-nosource", "No Source", homeostasis: 0.1);

        // negative template weight
        var badEra = new Era(
            Id: new EraId("era-genesis"),
            Name: "Genesis",
            Summary: "The beginning era",
            TemplateWeights: new Dictionary<string, double>
            {
                ["tmpl-spawn-faction"] = -1.0
            },
            SystemModifiers: new Dictionary<string, double> { ["sys-evolution"] = 1.0 },
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        // invalid relationship src kind
        var badRel = new RelationshipKindDefinition
        {
            Kind = new RelationshipKind("bad_rel"),
            Description = "Bad",
            SrcKinds = ["dragon"]
        };

        var updatedSchema = new DomainSchema
        {
            Id = config.Schema.Id,
            Name = config.Schema.Name,
            Version = config.Schema.Version,
            EntityKinds = config.Schema.EntityKinds,
            RelationshipKinds = config.Schema.RelationshipKinds.Append(badRel).ToList(),
            Cultures = config.Schema.Cultures
        };

        var updated = config with
        {
            Schema = updatedSchema,
            Templates = config.Templates.Append(orphan).ToList(),
            Systems = config.Systems.Append(orphanSys).ToList(),
            Pressures = [.. config.Pressures, noSink, noSource],
            Eras = [badEra]
        };

        var report = CoherenceValidator.Validate(updated);

        Assert.NotEmpty(report.Issues);
        Assert.All(report.Issues, issue =>
            Assert.Equal(CoherenceIssueSeverity.Warning, issue.Severity));
    }
}
