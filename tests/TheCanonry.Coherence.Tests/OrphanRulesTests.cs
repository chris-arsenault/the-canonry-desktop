using TheCanonry.Coherence.Types;
using TheCanonry.Engine.Engine;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Coherence.Tests;

/// <summary>
/// Tests for orphan-generators, orphan-systems, and zero-weight-generators coherence rules.
/// </summary>
public class OrphanRulesTests
{
    // =========================================================================
    // orphan-generators: CLEAN CASES
    // =========================================================================

    [Fact]
    public void AllTemplatesReferencedInEra_DoesNotTrigger()
    {
        // The minimal config's template is referenced in the era's TemplateWeights
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var report = CoherenceValidator.Validate(config);

        Assert.DoesNotContain(report.Issues, i => i.RuleId == "orphan-generators");
    }

    [Fact]
    public void DisabledTemplateNotInEra_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // Add a disabled template not referenced anywhere
        var disabledTemplate = CoherenceTestHelpers.CreateBareTemplate(
            "tmpl-disabled", "Disabled Template", enabled: false);

        var updated = config with
        {
            Templates = config.Templates.Append(disabledTemplate).ToList()
        };

        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "orphan-generators");
    }

    // =========================================================================
    // orphan-generators: TRIGGERING CASES
    // =========================================================================

    [Fact]
    public void EnabledTemplateNotInAnyEra_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var orphanTemplate = CoherenceTestHelpers.CreateBareTemplate(
            "tmpl-orphan", "Orphan Template", enabled: true);

        var updated = config with
        {
            Templates = config.Templates.Append(orphanTemplate).ToList()
        };

        var report = CoherenceValidator.Validate(updated);
        var issue = Assert.Single(report.Issues, i => i.RuleId == "orphan-generators");
        Assert.Equal(CoherenceIssueSeverity.Warning, issue.Severity);
        Assert.Single(issue.AffectedItems);
        Assert.Equal("tmpl-orphan", issue.AffectedItems[0].Id);
    }

    [Fact]
    public void MultipleOrphanTemplates_AllReported()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var orphan1 = CoherenceTestHelpers.CreateBareTemplate("tmpl-orphan-1", "Orphan 1");
        var orphan2 = CoherenceTestHelpers.CreateBareTemplate("tmpl-orphan-2", "Orphan 2");

        var updated = config with
        {
            Templates = config.Templates.Append(orphan1).Append(orphan2).ToList()
        };

        var report = CoherenceValidator.Validate(updated);
        var issue = Assert.Single(report.Issues, i => i.RuleId == "orphan-generators");
        Assert.Equal(2, issue.AffectedItems.Count);
        Assert.Contains(issue.AffectedItems, a => a.Id == "tmpl-orphan-1");
        Assert.Contains(issue.AffectedItems, a => a.Id == "tmpl-orphan-2");
    }

    // =========================================================================
    // orphan-systems: CLEAN CASES
    // =========================================================================

    [Fact]
    public void SystemReferencedInEra_DoesNotTrigger()
    {
        // The minimal config's system is referenced in the era's SystemModifiers
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var report = CoherenceValidator.Validate(config);

        Assert.DoesNotContain(report.Issues, i => i.RuleId == "orphan-systems");
    }

    [Fact]
    public void DisabledSystemNotInEra_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var disabledSystem = new DeclarativeSystem(
            SystemType: SystemType.GraphContagion,
            Enabled: false,
            Config: new Dictionary<string, object?>
            {
                ["id"] = "sys-disabled",
                ["name"] = "Disabled System"
            });

        var updated = config with
        {
            Systems = config.Systems.Append(disabledSystem).ToList()
        };

        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "orphan-systems");
    }

    // =========================================================================
    // orphan-systems: TRIGGERING CASES
    // =========================================================================

    [Fact]
    public void EnabledSystemNotInAnyEra_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var orphanSystem = new DeclarativeSystem(
            SystemType: SystemType.GraphContagion,
            Enabled: true,
            Config: new Dictionary<string, object?>
            {
                ["id"] = "sys-orphan",
                ["name"] = "Orphan System"
            });

        var updated = config with
        {
            Systems = config.Systems.Append(orphanSystem).ToList()
        };

        var report = CoherenceValidator.Validate(updated);
        var issue = Assert.Single(report.Issues, i => i.RuleId == "orphan-systems");
        Assert.Equal(CoherenceIssueSeverity.Warning, issue.Severity);
        Assert.Single(issue.AffectedItems);
        Assert.Equal("sys-orphan", issue.AffectedItems[0].Id);
    }

    [Fact]
    public void SystemWithoutIdInConfig_IsSkipped()
    {
        // Systems without an "id" key in their Config dict should be skipped, not crash
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var noIdSystem = new DeclarativeSystem(
            SystemType: SystemType.GraphContagion,
            Enabled: true,
            Config: new Dictionary<string, object?>
            {
                ["name"] = "No ID System"
            });

        var updated = config with
        {
            Systems = config.Systems.Append(noIdSystem).ToList()
        };

        // Should not throw, and should not report this system as orphan
        var report = CoherenceValidator.Validate(updated);
        var orphanIssue = report.Issues.FirstOrDefault(i => i.RuleId == "orphan-systems");
        if (orphanIssue is not null)
        {
            Assert.DoesNotContain(orphanIssue.AffectedItems, a => a.Label.Contains("No ID"));
        }
    }

    // =========================================================================
    // zero-weight-generators: CLEAN CASES
    // =========================================================================

    [Fact]
    public void TemplateWithPositiveWeight_DoesNotTrigger()
    {
        // The minimal config's template has weight 1.0
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var report = CoherenceValidator.Validate(config);

        Assert.DoesNotContain(report.Issues, i => i.RuleId == "zero-weight-generators");
    }

    [Fact]
    public void TemplateWithZeroInOneEraNonzeroInAnother_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var secondEra = new Era(
            Id: new EraId("era-expansion"),
            Name: "Expansion",
            Summary: "Growth era",
            TemplateWeights: new Dictionary<string, double> { ["tmpl-spawn-faction"] = 0.0 },
            SystemModifiers: new Dictionary<string, double>(),
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        var updated = config with
        {
            Eras = config.Eras.Append(secondEra).ToList()
        };

        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "zero-weight-generators");
    }

    // =========================================================================
    // zero-weight-generators: TRIGGERING CASES
    // =========================================================================

    [Fact]
    public void TemplateWithZeroWeightInAllEras_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // Set the template weight to 0 in the only era
        var eraWithZero = new Era(
            Id: new EraId("era-genesis"),
            Name: "Genesis",
            Summary: "The beginning era",
            TemplateWeights: new Dictionary<string, double> { ["tmpl-spawn-faction"] = 0.0 },
            SystemModifiers: new Dictionary<string, double> { ["sys-evolution"] = 1.0 },
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        var updated = config with { Eras = [eraWithZero] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "zero-weight-generators");
        Assert.Equal(CoherenceIssueSeverity.Warning, issue.Severity);
        Assert.Single(issue.AffectedItems);
        Assert.Equal("tmpl-spawn-faction", issue.AffectedItems[0].Id);
    }

    [Fact]
    public void TemplateWithZeroWeightAcrossMultipleEras_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var era1 = new Era(
            Id: new EraId("era-1"),
            Name: "Era One",
            Summary: "First era",
            TemplateWeights: new Dictionary<string, double> { ["tmpl-spawn-faction"] = 0.0 },
            SystemModifiers: new Dictionary<string, double> { ["sys-evolution"] = 1.0 },
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        var era2 = new Era(
            Id: new EraId("era-2"),
            Name: "Era Two",
            Summary: "Second era",
            TemplateWeights: new Dictionary<string, double> { ["tmpl-spawn-faction"] = 0.0 },
            SystemModifiers: new Dictionary<string, double>(),
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        var updated = config with { Eras = [era1, era2] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "zero-weight-generators");
        Assert.Single(issue.AffectedItems);
        Assert.Contains("2 era(s)", issue.AffectedItems[0].Detail);
    }

    [Fact]
    public void DisabledTemplateWithZeroWeight_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // Add a disabled template that is referenced with weight 0
        var disabledTemplate = CoherenceTestHelpers.CreateBareTemplate(
            "tmpl-disabled-zero", "Disabled Zero", enabled: false);

        var eraWithDisabledZero = new Era(
            Id: new EraId("era-genesis"),
            Name: "Genesis",
            Summary: "The beginning era",
            TemplateWeights: new Dictionary<string, double>
            {
                ["tmpl-spawn-faction"] = 1.0,
                ["tmpl-disabled-zero"] = 0.0
            },
            SystemModifiers: new Dictionary<string, double> { ["sys-evolution"] = 1.0 },
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        var updated = config with
        {
            Templates = config.Templates.Append(disabledTemplate).ToList(),
            Eras = [eraWithDisabledZero]
        };

        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "zero-weight-generators");
    }
}
