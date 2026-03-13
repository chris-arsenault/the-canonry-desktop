using TheCanonry.Coherence.Types;
using TheCanonry.Engine.Engine;

namespace TheCanonry.Coherence.Rules;

/// <summary>
/// Rules that validate numeric values are within acceptable ranges.
/// Out-of-range values indicate misconfiguration that may cause
/// unexpected simulation behavior.
/// </summary>
public static class NumericRangeRules
{
    /// <summary>
    /// Checks pressure initial values and homeostasis, plus era weight/modifier values.
    /// </summary>
    public static CoherenceIssue? NumericRangeIssues(EngineConfig config)
    {
        var affected = new List<AffectedItem>();

        // Pressure checks
        foreach (var pressure in config.Pressures)
        {
            if (pressure.InitialValue is < -100 or > 100)
            {
                affected.Add(new AffectedItem(
                    pressure.Id,
                    pressure.Name,
                    $"InitialValue {pressure.InitialValue} is outside the valid range [-100, 100]"));
            }

            if (pressure.Homeostasis < 0)
            {
                affected.Add(new AffectedItem(
                    pressure.Id,
                    pressure.Name,
                    $"Homeostasis {pressure.Homeostasis} is negative; must be >= 0"));
            }
        }

        // Era checks
        foreach (var era in config.Eras)
        {
            foreach (var (templateId, weight) in era.TemplateWeights)
            {
                if (weight < 0)
                {
                    affected.Add(new AffectedItem(
                        era.Id.Value,
                        $"Era '{era.Name}'",
                        $"TemplateWeight for '{templateId}' is {weight}; must be >= 0"));
                }
            }

            foreach (var (systemId, modifier) in era.SystemModifiers)
            {
                if (modifier < 0)
                {
                    affected.Add(new AffectedItem(
                        era.Id.Value,
                        $"Era '{era.Name}'",
                        $"SystemModifier for '{systemId}' is {modifier}; must be >= 0"));
                }
            }
        }

        if (affected.Count == 0) return null;

        return new CoherenceIssue(
            "numeric-range-issues",
            "Out-of-range numeric values",
            $"{affected.Count} value(s) are outside their expected numeric ranges. " +
            "This may cause unpredictable simulation behavior.",
            CoherenceIssueSeverity.Warning,
            affected);
    }

    /// <summary>
    /// Checks that SrcKinds and DstKinds on RelationshipKindDefinitions
    /// reference entity kinds that exist in the schema.
    /// </summary>
    public static CoherenceIssue? RelationshipCompatibility(EngineConfig config)
    {
        var validKinds = new HashSet<string>();
        foreach (var ekd in config.Schema.EntityKinds)
        {
            validKinds.Add(ekd.Kind.Value);
        }

        var affected = new List<AffectedItem>();

        foreach (var relDef in config.Schema.RelationshipKinds)
        {
            foreach (var srcKind in relDef.SrcKinds)
            {
                if (!validKinds.Contains(srcKind))
                {
                    affected.Add(new AffectedItem(
                        relDef.Kind.Value,
                        $"Relationship '{relDef.Kind.Value}'",
                        $"SrcKinds entry '{srcKind}' is not a defined entity kind"));
                }
            }

            foreach (var dstKind in relDef.DstKinds)
            {
                if (!validKinds.Contains(dstKind))
                {
                    affected.Add(new AffectedItem(
                        relDef.Kind.Value,
                        $"Relationship '{relDef.Kind.Value}'",
                        $"DstKinds entry '{dstKind}' is not a defined entity kind"));
                }
            }
        }

        if (affected.Count == 0) return null;

        return new CoherenceIssue(
            "relationship-compatibility",
            "Invalid entity kind references in relationships",
            $"{affected.Count} relationship kind constraint(s) reference entity kinds " +
            "not defined in the schema. Relationship validation will not work correctly.",
            CoherenceIssueSeverity.Warning,
            affected);
    }
}
