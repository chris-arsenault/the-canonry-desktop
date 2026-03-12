namespace TheCanonry.Coherence.Tests;

using TheCanonry.Coherence.Types;
using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Templates;

/// <summary>
/// Tests for pressure-without-sources and pressure-without-sinks coherence rules.
/// </summary>
public class PressureRulesTests
{
    // =========================================================================
    // pressure-without-sources: CLEAN CASES
    // =========================================================================

    [Fact]
    public void PressureWithTemplateMutationSource_DoesNotTrigger()
    {
        // The minimal config already has a template with ModifyPressureMutation(+5)
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var report = CoherenceValidator.Validate(config);

        Assert.DoesNotContain(report.Issues, i => i.RuleId == "pressure-without-sources");
    }

    [Fact]
    public void PressureWithPositiveFeedback_DoesNotTrigger()
    {
        // Pressure has no template source, but has PositiveFeedback
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // Replace the template's state updates to remove the pressure mutation
        var templateWithoutMutation = config.Templates[0] with { StateUpdates = [] };

        // Replace the pressure to have positive feedback
        var pressureWithFeedback = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure",
            homeostasis: 0.05,
            positiveFeedback: [new EntityCountMetric("faction", "merchant", "active", 1.0, 100)]);

        var updated = config with
        {
            Templates = [templateWithoutMutation],
            Pressures = [pressureWithFeedback]
        };

        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "pressure-without-sources");
    }

    // =========================================================================
    // pressure-without-sources: TRIGGERING CASES
    // =========================================================================

    [Fact]
    public void PressureWithNoSourcesOrFeedback_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // Remove the template mutation and leave pressure with no feedback
        var templateWithoutMutation = config.Templates[0] with { StateUpdates = [] };
        var inertPressure = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure", homeostasis: 0.05);

        var updated = config with
        {
            Templates = [templateWithoutMutation],
            Pressures = [inertPressure]
        };

        var report = CoherenceValidator.Validate(updated);
        var issue = Assert.Single(report.Issues, i => i.RuleId == "pressure-without-sources");
        Assert.Equal(CoherenceIssueSeverity.Warning, issue.Severity);
        Assert.Single(issue.AffectedItems);
        Assert.Equal("pressure-conflict", issue.AffectedItems[0].Id);
    }

    [Fact]
    public void MultiplePressuresWithoutSources_AllReported()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var templateWithoutMutation = config.Templates[0] with { StateUpdates = [] };

        var updated = config with
        {
            Templates = [templateWithoutMutation],
            Pressures =
            [
                CoherenceTestHelpers.CreatePressure("p1", "Pressure One"),
                CoherenceTestHelpers.CreatePressure("p2", "Pressure Two")
            ]
        };

        var report = CoherenceValidator.Validate(updated);
        var issue = Assert.Single(report.Issues, i => i.RuleId == "pressure-without-sources");
        Assert.Equal(2, issue.AffectedItems.Count);
    }

    [Fact]
    public void PressureSourceInVariant_DoesNotTrigger()
    {
        // A pressure mutation inside a template variant should count as a source
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var variant = new TemplateVariant(
            Name: "War Variant",
            When: new PressureCondition("pressure-conflict", 10, 100),
            Apply: new VariantEffects(
                Subtype: new Dictionary<string, string>(),
                Tags: new Dictionary<string, IReadOnlyDictionary<string, bool>>(),
                Relationships: [],
                StateUpdates: [new ModifyPressureMutation("pressure-conflict", 10.0)]));

        // Remove top-level mutations, add variant with the mutation
        var templateWithVariant = config.Templates[0] with
        {
            StateUpdates = [],
            Variants = new TemplateVariants(VariantSelectionMode.FirstMatch, [variant])
        };

        var updated = config with { Templates = [templateWithVariant] };
        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "pressure-without-sources");
    }

    // =========================================================================
    // pressure-without-sinks: CLEAN CASES
    // =========================================================================

    [Fact]
    public void PressureWithHomeostasis_DoesNotTriggerSinks()
    {
        // The minimal config has homeostasis > 0
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var report = CoherenceValidator.Validate(config);

        Assert.DoesNotContain(report.Issues, i => i.RuleId == "pressure-without-sinks");
    }

    [Fact]
    public void PressureWithNegativeMutationSink_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // Pressure with zero homeostasis but a negative template mutation
        var pressureNoHomeo = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure", homeostasis: 0);

        var templateWithNegMutation = config.Templates[0] with
        {
            StateUpdates =
            [
                new ModifyPressureMutation("pressure-conflict", 5.0),
                new ModifyPressureMutation("pressure-conflict", -3.0)
            ]
        };

        var updated = config with
        {
            Templates = [templateWithNegMutation],
            Pressures = [pressureNoHomeo]
        };

        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "pressure-without-sinks");
    }

    [Fact]
    public void PressureWithNegativeFeedback_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var pressureWithNegFeedback = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure",
            homeostasis: 0,
            negativeFeedback: [new EntityCountMetric("faction", "merchant", "active", 1.0, 100)]);

        var updated = config with { Pressures = [pressureWithNegFeedback] };
        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "pressure-without-sinks");
    }

    // =========================================================================
    // pressure-without-sinks: TRIGGERING CASES
    // =========================================================================

    [Fact]
    public void PressureWithNoSinksOrHomeostasis_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // Pressure with zero homeostasis, no negative feedback, no negative mutations
        var pressureNoSink = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure", homeostasis: 0);

        var updated = config with { Pressures = [pressureNoSink] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "pressure-without-sinks");
        Assert.Equal(CoherenceIssueSeverity.Warning, issue.Severity);
        Assert.Single(issue.AffectedItems);
        Assert.Equal("pressure-conflict", issue.AffectedItems[0].Id);
    }

    [Fact]
    public void PressureSinkInVariant_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var variant = new TemplateVariant(
            Name: "Peace Variant",
            When: new PressureCondition("pressure-conflict", -100, -10),
            Apply: new VariantEffects(
                Subtype: new Dictionary<string, string>(),
                Tags: new Dictionary<string, IReadOnlyDictionary<string, bool>>(),
                Relationships: [],
                StateUpdates: [new ModifyPressureMutation("pressure-conflict", -10.0)]));

        var pressureNoHomeo = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure", homeostasis: 0);

        var templateWithVariantSink = config.Templates[0] with
        {
            StateUpdates = [new ModifyPressureMutation("pressure-conflict", 5.0)],
            Variants = new TemplateVariants(VariantSelectionMode.FirstMatch, [variant])
        };

        var updated = config with
        {
            Templates = [templateWithVariantSink],
            Pressures = [pressureNoHomeo]
        };

        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "pressure-without-sinks");
    }

    // =========================================================================
    // NO PRESSURES: CLEAN
    // =========================================================================

    [Fact]
    public void NoPressures_NeitherRuleTriggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var updated = config with { Pressures = [] };
        var report = CoherenceValidator.Validate(updated);

        Assert.DoesNotContain(report.Issues, i => i.RuleId == "pressure-without-sources");
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "pressure-without-sinks");
    }
}
