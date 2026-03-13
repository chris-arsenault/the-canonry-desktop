using TheCanonry.Coherence.Types;
using TheCanonry.Engine.Templates;
using TheCanonry.Schema.Config;

namespace TheCanonry.Coherence.Tests;

/// <summary>
/// Tests for invalid-subtype-references, invalid-status-references,
/// and invalid-culture-references coherence rules.
/// </summary>
public class CrossReferenceRulesTests
{
    // =========================================================================
    // invalid-subtype-references: CLEAN CASES
    // =========================================================================

    [Fact]
    public void ValidFixedSubtype_DoesNotTrigger()
    {
        // The minimal config uses FixedSubtype("merchant") which is defined
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var report = CoherenceValidator.Validate(config);

        Assert.DoesNotContain(report.Issues, i => i.RuleId == "invalid-subtype-references");
    }

    [Fact]
    public void ValidRandomSubtype_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var creationWithRandom = config.Templates[0].Creation[0] with
        {
            Subtype = new RandomSubtype(["merchant", "military"])
        };
        var templateWithRandom = config.Templates[0] with { Creation = [creationWithRandom] };

        var updated = config with { Templates = [templateWithRandom] };
        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "invalid-subtype-references");
    }

    [Fact]
    public void ValidConditionalSubtype_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var creationWithConditional = config.Templates[0].Creation[0] with
        {
            Subtype = new ConditionalSubtype(
                When: [new SubtypeWhen(
                    Condition: new TargetSubtypeCondition("merchant"),
                    Then: "military")],
                Otherwise: "merchant")
        };
        var templateWithConditional = config.Templates[0] with { Creation = [creationWithConditional] };

        var updated = config with { Templates = [templateWithConditional] };
        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "invalid-subtype-references");
    }

    // =========================================================================
    // invalid-subtype-references: TRIGGERING CASES
    // =========================================================================

    [Fact]
    public void InvalidFixedSubtype_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badCreation = config.Templates[0].Creation[0] with
        {
            Subtype = new FixedSubtype("pirate") // not in schema
        };
        var badTemplate = config.Templates[0] with { Creation = [badCreation] };

        var updated = config with { Templates = [badTemplate] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "invalid-subtype-references");
        Assert.Equal(CoherenceIssueSeverity.Warning, issue.Severity);
        Assert.Single(issue.AffectedItems);
        Assert.Contains("pirate", issue.AffectedItems[0].Detail);
    }

    [Fact]
    public void InvalidRandomSubtype_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badCreation = config.Templates[0].Creation[0] with
        {
            Subtype = new RandomSubtype(["merchant", "pirate"]) // "pirate" not in schema
        };
        var badTemplate = config.Templates[0] with { Creation = [badCreation] };

        var updated = config with { Templates = [badTemplate] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "invalid-subtype-references");
        Assert.Single(issue.AffectedItems);
        Assert.Contains("pirate", issue.AffectedItems[0].Detail);
    }

    [Fact]
    public void InvalidConditionalSubtype_Otherwise_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badCreation = config.Templates[0].Creation[0] with
        {
            Subtype = new ConditionalSubtype(
                When: [new SubtypeWhen(
                    Condition: new TargetSubtypeCondition("merchant"),
                    Then: "military")],
                Otherwise: "nomad") // not in schema
        };
        var badTemplate = config.Templates[0] with { Creation = [badCreation] };

        var updated = config with { Templates = [badTemplate] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "invalid-subtype-references");
        Assert.Contains("nomad", issue.AffectedItems[0].Detail);
    }

    [Fact]
    public void InvalidConditionalSubtype_When_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badCreation = config.Templates[0].Creation[0] with
        {
            Subtype = new ConditionalSubtype(
                When: [new SubtypeWhen(
                    Condition: new TargetSubtypeCondition("merchant"),
                    Then: "raider")], // not in schema
                Otherwise: "merchant")
        };
        var badTemplate = config.Templates[0] with { Creation = [badCreation] };

        var updated = config with { Templates = [badTemplate] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "invalid-subtype-references");
        Assert.Contains("raider", issue.AffectedItems[0].Detail);
    }

    [Fact]
    public void InheritSubtype_NotValidatedStatically()
    {
        // InheritSubtype cannot be validated statically, should not trigger
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var creationWithInherit = config.Templates[0].Creation[0] with
        {
            Subtype = new InheritSubtype("$target", 1.0)
        };
        var template = config.Templates[0] with { Creation = [creationWithInherit] };

        var updated = config with { Templates = [template] };
        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "invalid-subtype-references");
    }

    [Fact]
    public void AffectedItemIdentifiesTemplate()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badCreation = config.Templates[0].Creation[0] with
        {
            Subtype = new FixedSubtype("invalid")
        };
        var badTemplate = config.Templates[0] with { Creation = [badCreation] };

        var updated = config with { Templates = [badTemplate] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "invalid-subtype-references");
        Assert.Equal("tmpl-spawn-faction", issue.AffectedItems[0].Id);
    }

    // =========================================================================
    // invalid-status-references: CLEAN CASES
    // =========================================================================

    [Fact]
    public void ValidStatus_DoesNotTrigger()
    {
        // The minimal config uses status "active" which is defined
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var report = CoherenceValidator.Validate(config);

        Assert.DoesNotContain(report.Issues, i => i.RuleId == "invalid-status-references");
    }

    // =========================================================================
    // invalid-status-references: TRIGGERING CASES
    // =========================================================================

    [Fact]
    public void InvalidStatus_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badCreation = config.Templates[0].Creation[0] with
        {
            Status = "dormant" // not defined for faction
        };
        var badTemplate = config.Templates[0] with { Creation = [badCreation] };

        var updated = config with { Templates = [badTemplate] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "invalid-status-references");
        Assert.Equal(CoherenceIssueSeverity.Warning, issue.Severity);
        Assert.Single(issue.AffectedItems);
        Assert.Contains("dormant", issue.AffectedItems[0].Detail);
        Assert.Contains("faction", issue.AffectedItems[0].Detail);
    }

    [Fact]
    public void InvalidStatus_AffectedItemIdentifiesTemplateAndEntityRef()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var badCreation = config.Templates[0].Creation[0] with
        {
            Status = "destroyed"
        };
        var badTemplate = config.Templates[0] with { Creation = [badCreation] };

        var updated = config with { Templates = [badTemplate] };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "invalid-status-references");
        Assert.Equal("tmpl-spawn-faction", issue.AffectedItems[0].Id);
        Assert.Contains("$newFaction", issue.AffectedItems[0].Label);
    }

    // =========================================================================
    // invalid-culture-references: CLEAN CASES
    // =========================================================================

    [Fact]
    public void NoCultureReferences_DoesNotTrigger()
    {
        // The minimal config has no semantic planes with regions
        var config = CoherenceTestHelpers.CreateMinimalConfig();
        var report = CoherenceValidator.Validate(config);

        Assert.DoesNotContain(report.Issues, i => i.RuleId == "invalid-culture-references");
    }

    [Fact]
    public void ValidCultureInSemanticRegion_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var factionKind = config.Schema.EntityKinds.First(e => e.Kind.Value == "faction");
        var updatedFactionKind = new EntityKindDefinition
        {
            Kind = factionKind.Kind,
            Description = factionKind.Description,
            Category = factionKind.Category,
            Subtypes = factionKind.Subtypes,
            Statuses = factionKind.Statuses,
            SemanticPlane = new SemanticPlane
            {
                Axes = new SemanticPlaneAxes
                {
                    X = new SemanticAxis { AxisId = "power" },
                    Y = new SemanticAxis { AxisId = "wealth" },
                    Z = new SemanticAxis { AxisId = "influence" }
                },
                Regions =
                [
                    new SemanticRegion
                    {
                        Id = "region-test",
                        Label = "Test Region",
                        Color = "#fff",
                        Culture = "test" // valid culture
                    }
                ]
            }
        };

        var updatedEntityKinds = config.Schema.EntityKinds
            .Where(e => e.Kind.Value != "faction")
            .Append(updatedFactionKind)
            .ToList();

        var updatedSchema = new DomainSchema
        {
            Id = config.Schema.Id,
            Name = config.Schema.Name,
            Version = config.Schema.Version,
            EntityKinds = updatedEntityKinds,
            RelationshipKinds = config.Schema.RelationshipKinds,
            Cultures = config.Schema.Cultures
        };

        var updated = config with { Schema = updatedSchema };
        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "invalid-culture-references");
    }

    [Fact]
    public void EmptyCultureInSemanticRegion_DoesNotTrigger()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var factionKind = config.Schema.EntityKinds.First(e => e.Kind.Value == "faction");
        var updatedFactionKind = new EntityKindDefinition
        {
            Kind = factionKind.Kind,
            Description = factionKind.Description,
            Category = factionKind.Category,
            Subtypes = factionKind.Subtypes,
            Statuses = factionKind.Statuses,
            SemanticPlane = new SemanticPlane
            {
                Axes = new SemanticPlaneAxes
                {
                    X = new SemanticAxis { AxisId = "power" },
                    Y = new SemanticAxis { AxisId = "wealth" },
                    Z = new SemanticAxis { AxisId = "influence" }
                },
                Regions =
                [
                    new SemanticRegion
                    {
                        Id = "region-neutral",
                        Label = "Neutral Region",
                        Color = "#ccc",
                        Culture = "" // empty culture is allowed
                    }
                ]
            }
        };

        var updatedEntityKinds = config.Schema.EntityKinds
            .Where(e => e.Kind.Value != "faction")
            .Append(updatedFactionKind)
            .ToList();

        var updatedSchema = new DomainSchema
        {
            Id = config.Schema.Id,
            Name = config.Schema.Name,
            Version = config.Schema.Version,
            EntityKinds = updatedEntityKinds,
            RelationshipKinds = config.Schema.RelationshipKinds,
            Cultures = config.Schema.Cultures
        };

        var updated = config with { Schema = updatedSchema };
        var report = CoherenceValidator.Validate(updated);
        Assert.DoesNotContain(report.Issues, i => i.RuleId == "invalid-culture-references");
    }

    // =========================================================================
    // invalid-culture-references: TRIGGERING CASES
    // =========================================================================

    [Fact]
    public void InvalidCultureInSemanticRegion_Triggers()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var factionKind = config.Schema.EntityKinds.First(e => e.Kind.Value == "faction");
        var updatedFactionKind = new EntityKindDefinition
        {
            Kind = factionKind.Kind,
            Description = factionKind.Description,
            Category = factionKind.Category,
            Subtypes = factionKind.Subtypes,
            Statuses = factionKind.Statuses,
            SemanticPlane = new SemanticPlane
            {
                Axes = new SemanticPlaneAxes
                {
                    X = new SemanticAxis { AxisId = "power" },
                    Y = new SemanticAxis { AxisId = "wealth" },
                    Z = new SemanticAxis { AxisId = "influence" }
                },
                Regions =
                [
                    new SemanticRegion
                    {
                        Id = "region-invalid",
                        Label = "Invalid Region",
                        Color = "#f00",
                        Culture = "nonexistent_culture" // not in schema
                    }
                ]
            }
        };

        var updatedEntityKinds = config.Schema.EntityKinds
            .Where(e => e.Kind.Value != "faction")
            .Append(updatedFactionKind)
            .ToList();

        var updatedSchema = new DomainSchema
        {
            Id = config.Schema.Id,
            Name = config.Schema.Name,
            Version = config.Schema.Version,
            EntityKinds = updatedEntityKinds,
            RelationshipKinds = config.Schema.RelationshipKinds,
            Cultures = config.Schema.Cultures
        };

        var updated = config with { Schema = updatedSchema };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "invalid-culture-references");
        Assert.Equal(CoherenceIssueSeverity.Warning, issue.Severity);
        Assert.Single(issue.AffectedItems);
        Assert.Equal("region-invalid", issue.AffectedItems[0].Id);
        Assert.Contains("nonexistent_culture", issue.AffectedItems[0].Detail);
    }

    [Fact]
    public void MultipleInvalidCultureRegions_AllReported()
    {
        var config = CoherenceTestHelpers.CreateMinimalConfig();

        var factionKind = config.Schema.EntityKinds.First(e => e.Kind.Value == "faction");
        var updatedFactionKind = new EntityKindDefinition
        {
            Kind = factionKind.Kind,
            Description = factionKind.Description,
            Category = factionKind.Category,
            Subtypes = factionKind.Subtypes,
            Statuses = factionKind.Statuses,
            SemanticPlane = new SemanticPlane
            {
                Axes = new SemanticPlaneAxes
                {
                    X = new SemanticAxis { AxisId = "power" },
                    Y = new SemanticAxis { AxisId = "wealth" },
                    Z = new SemanticAxis { AxisId = "influence" }
                },
                Regions =
                [
                    new SemanticRegion
                    {
                        Id = "region-a",
                        Label = "Region A",
                        Color = "#f00",
                        Culture = "bad_culture_1"
                    },
                    new SemanticRegion
                    {
                        Id = "region-b",
                        Label = "Region B",
                        Color = "#0f0",
                        Culture = "bad_culture_2"
                    }
                ]
            }
        };

        var updatedEntityKinds = config.Schema.EntityKinds
            .Where(e => e.Kind.Value != "faction")
            .Append(updatedFactionKind)
            .ToList();

        var updatedSchema = new DomainSchema
        {
            Id = config.Schema.Id,
            Name = config.Schema.Name,
            Version = config.Schema.Version,
            EntityKinds = updatedEntityKinds,
            RelationshipKinds = config.Schema.RelationshipKinds,
            Cultures = config.Schema.Cultures
        };

        var updated = config with { Schema = updatedSchema };
        var report = CoherenceValidator.Validate(updated);

        var issue = Assert.Single(report.Issues, i => i.RuleId == "invalid-culture-references");
        Assert.Equal(2, issue.AffectedItems.Count);
        Assert.Contains(issue.AffectedItems, a => a.Id == "region-a");
        Assert.Contains(issue.AffectedItems, a => a.Id == "region-b");
    }
}
