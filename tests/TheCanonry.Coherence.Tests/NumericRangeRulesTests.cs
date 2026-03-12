namespace TheCanonry.Coherence.Tests;

using TheCanonry.Coherence.Types;
using TheCanonry.Engine.Engine;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;

/// <summary>
/// Tests for numeric-range-issues and relationship-compatibility coherence rules.
/// </summary>
public class NumericRangeRulesTests
{
    // =========================================================================
    // numeric-range-issues: CLEAN CASES
    // =========================================================================

    [Fact]
    public void ValidNumericRanges_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var report = CoherenceValidator.Validate(config);

        Assert.DoesNotContain(report.Issues, i => i.RuleId == "numeric-range-issues");
    }

    [Fact]
    public void PressureAtBoundaryValues_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // InitialValue at exact boundaries
        var pressureAtMax = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure",
            homeostasis: 0.05,
            initialValue: 100);

        var updated = config with { Pressures = [pressureAtMax] };
        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "numeric-range-issues");
    }

    [Fact]
    public void PressureAtNegativeBoundary_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var pressureAtMin = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure",
            homeostasis: 0.05,
            initialValue: -100);

        var updated = config with { Pressures = [pressureAtMin] };
        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "numeric-range-issues");
    }

    [Fact]
    public void ZeroHomeostasis_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var pressureZeroHomeo = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure",
            homeostasis: 0);

        var updated = config with { Pressures = [pressureZeroHomeo] };
        var report = CoherenceValidator.Validate(updated);

        // numeric-range-issues should not flag homeostasis = 0
        var numericIssue = report.Issues.FirstOrDefault(i => i.RuleId == "numeric-range-issues");
        if (numericIssue is not null)
        {
            Assert.DoesNotContain(numericIssue.AffectedItems,
                a => a.Detail.Contains("Homeostasis"));
        }
    }

    // =========================================================================
    // numeric-range-issues: PRESSURE INITIAL VALUE
    // =========================================================================

    [Fact]
    public void PressureInitialValueAbove100_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var outOfRange = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure",
            homeostasis: 0.05,
            initialValue: 150);

        var updated = config with { Pressures = [outOfRange] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "numeric-range-issues");
        Assert.Equal(CoherenceIssueSeverity.Warning, issue.Severity);
        Assert.Contains(issue.AffectedItems, a => a.Detail.Contains("InitialValue"));
    }

    [Fact]
    public void PressureInitialValueBelowNeg100_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var outOfRange = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure",
            homeostasis: 0.05,
            initialValue: -200);

        var updated = config with { Pressures = [outOfRange] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "numeric-range-issues");
        Assert.Contains(issue.AffectedItems, a => a.Detail.Contains("InitialValue"));
    }

    // =========================================================================
    // numeric-range-issues: NEGATIVE HOMEOSTASIS
    // =========================================================================

    [Fact]
    public void NegativeHomeostasis_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var negHomeo = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure",
            homeostasis: -0.1,
            initialValue: 0);

        var updated = config with { Pressures = [negHomeo] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "numeric-range-issues");
        Assert.Contains(issue.AffectedItems, a => a.Detail.Contains("Homeostasis"));
    }

    // =========================================================================
    // numeric-range-issues: ERA TEMPLATE WEIGHTS
    // =========================================================================

    [Fact]
    public void NegativeTemplateWeight_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badEra = new Era(
            Id: new EraId("era-genesis"),
            Name: "Genesis",
            Summary: "The beginning era",
            TemplateWeights: new Dictionary<string, double> { ["tmpl-spawn-faction"] = -1.0 },
            SystemModifiers: new Dictionary<string, double> { ["sys-evolution"] = 1.0 },
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        var updated = config with { Eras = [badEra] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "numeric-range-issues");
        Assert.Contains(issue.AffectedItems,
            a => a.Detail.Contains("TemplateWeight") && a.Detail.Contains("tmpl-spawn-faction"));
    }

    // =========================================================================
    // numeric-range-issues: ERA SYSTEM MODIFIERS
    // =========================================================================

    [Fact]
    public void NegativeSystemModifier_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badEra = new Era(
            Id: new EraId("era-genesis"),
            Name: "Genesis",
            Summary: "The beginning era",
            TemplateWeights: new Dictionary<string, double> { ["tmpl-spawn-faction"] = 1.0 },
            SystemModifiers: new Dictionary<string, double> { ["sys-evolution"] = -2.0 },
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        var updated = config with { Eras = [badEra] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "numeric-range-issues");
        Assert.Contains(issue.AffectedItems,
            a => a.Detail.Contains("SystemModifier") && a.Detail.Contains("sys-evolution"));
    }

    // =========================================================================
    // numeric-range-issues: MULTIPLE VIOLATIONS
    // =========================================================================

    [Fact]
    public void MultipleNumericViolations_AllCombinedInSingleIssue()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        // Pressure out of range AND negative homeostasis
        var badPressure = CoherenceTestHelpers.CreatePressure(
            "pressure-conflict", "Conflict Pressure",
            homeostasis: -0.5,
            initialValue: 200);

        // Era with negative weight
        var badEra = new Era(
            Id: new EraId("era-genesis"),
            Name: "Genesis",
            Summary: "The beginning era",
            TemplateWeights: new Dictionary<string, double> { ["tmpl-spawn-faction"] = -1.0 },
            SystemModifiers: new Dictionary<string, double> { ["sys-evolution"] = 1.0 },
            PressureModifiers: new Dictionary<string, double>(),
            ExitConditions: [],
            EntryConditions: [],
            NextEra: null,
            ExitEffects: EraTransitionEffects.Empty,
            EntryEffects: EraTransitionEffects.Empty);

        var updated = config with
        {
            Pressures = [badPressure],
            Eras = [badEra]
        };

        var report = CoherenceValidator.Validate(updated);
        var issue = Assert.Single(report.Issues, i => i.RuleId == "numeric-range-issues");

        // Should have at least 3 items: initialValue, homeostasis, template weight
        Assert.True(issue.AffectedItems.Count >= 3,
            $"Expected at least 3 affected items, got {issue.AffectedItems.Count}");
    }

    // =========================================================================
    // relationship-compatibility: CLEAN CASES
    // =========================================================================

    [Fact]
    public void ValidSrcDstKinds_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var report = CoherenceValidator.Validate(config);

        Assert.DoesNotContain(report.Issues, i => i.RuleId == "relationship-compatibility");
    }

    [Fact]
    public void EmptySrcDstKinds_DoesNotTrigger()
    {
        // The minimal config's "allied_with" has empty SrcKinds/DstKinds (no constraints)
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var report = CoherenceValidator.Validate(config);

        Assert.DoesNotContain(report.Issues, i => i.RuleId == "relationship-compatibility");
    }

    [Fact]
    public void ValidConstrainedRelationship_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var constrainedRel = new RelationshipKindDefinition
        {
            Kind = new RelationshipKind("trade_with"),
            Description = "Trade relationship",
            SrcKinds = ["faction"],   // valid entity kind
            DstKinds = ["faction"]    // valid entity kind
        };

        var updatedSchema = new DomainSchema
        {
            Id = config.Schema.Id,
            Name = config.Schema.Name,
            Version = config.Schema.Version,
            EntityKinds = config.Schema.EntityKinds,
            RelationshipKinds = config.Schema.RelationshipKinds.Append(constrainedRel).ToList(),
            Cultures = config.Schema.Cultures
        };

        var updated = config with { Schema = updatedSchema };
        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "relationship-compatibility");
    }

    // =========================================================================
    // relationship-compatibility: TRIGGERING CASES
    // =========================================================================

    [Fact]
    public void InvalidSrcKind_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badRel = new RelationshipKindDefinition
        {
            Kind = new RelationshipKind("controls"),
            Description = "Control relationship",
            SrcKinds = ["dragon"], // not a defined entity kind
            DstKinds = ["faction"]
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

        var updated = config with { Schema = updatedSchema };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "relationship-compatibility");
        Assert.Equal(CoherenceIssueSeverity.Warning, issue.Severity);
        Assert.Single(issue.AffectedItems);
        Assert.Contains("dragon", issue.AffectedItems[0].Detail);
        Assert.Contains("SrcKinds", issue.AffectedItems[0].Detail);
    }

    [Fact]
    public void InvalidDstKind_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badRel = new RelationshipKindDefinition
        {
            Kind = new RelationshipKind("worships"),
            Description = "Worship relationship",
            SrcKinds = ["faction"],
            DstKinds = ["deity"] // not defined
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

        var updated = config with { Schema = updatedSchema };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "relationship-compatibility");
        Assert.Contains("deity", issue.AffectedItems[0].Detail);
        Assert.Contains("DstKinds", issue.AffectedItems[0].Detail);
    }

    [Fact]
    public void BothSrcAndDstInvalid_BothReported()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badRel = new RelationshipKindDefinition
        {
            Kind = new RelationshipKind("mythic_bond"),
            Description = "A mythic bond",
            SrcKinds = ["hero"],   // invalid
            DstKinds = ["villain"] // invalid
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

        var updated = config with { Schema = updatedSchema };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "relationship-compatibility");
        Assert.Equal(2, issue.AffectedItems.Count);
        Assert.Contains(issue.AffectedItems, a => a.Detail.Contains("hero"));
        Assert.Contains(issue.AffectedItems, a => a.Detail.Contains("villain"));
    }

    [Fact]
    public void AffectedItemIdentifiesRelationshipKind()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badRel = new RelationshipKindDefinition
        {
            Kind = new RelationshipKind("obeys"),
            Description = "Obedience",
            SrcKinds = ["minion"] // invalid
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

        var updated = config with { Schema = updatedSchema };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "relationship-compatibility");
        Assert.Equal("obeys", issue.AffectedItems[0].Id);
    }
}
