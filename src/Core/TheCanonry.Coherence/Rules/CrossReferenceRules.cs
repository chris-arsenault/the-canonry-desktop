using TheCanonry.Coherence.Types;
using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Templates;
using TheCanonry.Schema.Config;

namespace TheCanonry.Coherence.Rules;

/// <summary>
/// Rules that validate cross-references between configuration elements.
/// Templates reference subtypes, statuses, and cultures defined in the schema;
/// these rules verify that all references resolve.
/// </summary>
public static class CrossReferenceRules
{
    /// <summary>
    /// Detects creation rules that reference subtypes not defined in the schema
    /// for the corresponding entity kind.
    /// </summary>
    public static CoherenceIssue? InvalidSubtypeReferences(EngineConfig config)
    {
        var subtypesByKind = BuildSubtypeIndex(config.Schema);
        var affected = new List<AffectedItem>();

        foreach (var template in config.Templates)
        {
            foreach (var rule in template.Creation)
            {
                if (!subtypesByKind.TryGetValue(rule.Kind, out var validSubtypes))
                {
                    // Kind itself is invalid -- a different rule could catch this
                    continue;
                }

                CheckSubtypeSpec(template.Id, rule.Kind, rule.Subtype, validSubtypes, affected);
            }
        }

        if (affected.Count == 0) return null;

        return new CoherenceIssue(
            "invalid-subtype-references",
            "Invalid subtype references",
            $"{affected.Count} creation rule(s) reference subtypes not defined in the schema. " +
            "Entity creation will fail or produce unexpected subtypes at runtime.",
            CoherenceIssueSeverity.Warning,
            affected);
    }

    /// <summary>
    /// Detects creation rules that reference statuses not defined in the schema
    /// for the corresponding entity kind.
    /// </summary>
    public static CoherenceIssue? InvalidStatusReferences(EngineConfig config)
    {
        var statusesByKind = BuildStatusIndex(config.Schema);
        var affected = new List<AffectedItem>();

        foreach (var template in config.Templates)
        {
            foreach (var rule in template.Creation)
            {
                if (!statusesByKind.TryGetValue(rule.Kind, out var validStatuses))
                    continue;

                if (!validStatuses.Contains(rule.Status))
                {
                    affected.Add(new AffectedItem(
                        template.Id,
                        $"{template.Id} / {rule.EntityRef}",
                        $"Status '{rule.Status}' is not defined for kind '{rule.Kind}'"));
                }
            }
        }

        if (affected.Count == 0) return null;

        return new CoherenceIssue(
            "invalid-status-references",
            "Invalid status references",
            $"{affected.Count} creation rule(s) reference statuses not defined in the schema. " +
            "Entity creation will use an unrecognized status value.",
            CoherenceIssueSeverity.Warning,
            affected);
    }

    /// <summary>
    /// Detects semantic regions whose Culture string does not match any
    /// CultureDefinition.Id in the schema.
    /// </summary>
    public static CoherenceIssue? InvalidCultureReferences(EngineConfig config)
    {
        var validCultures = new HashSet<string>();
        foreach (var culture in config.Schema.Cultures)
        {
            validCultures.Add(culture.Id.Value);
        }

        var affected = new List<AffectedItem>();

        foreach (var entityKind in config.Schema.EntityKinds)
        {
            if (entityKind.SemanticPlane is null) continue;

            foreach (var region in entityKind.SemanticPlane.Regions)
            {
                if (string.IsNullOrEmpty(region.Culture)) continue;

                if (!validCultures.Contains(region.Culture))
                {
                    affected.Add(new AffectedItem(
                        region.Id,
                        $"{entityKind.Kind.Value} / {region.Label}",
                        $"Culture '{region.Culture}' is not defined in the schema"));
                }
            }
        }

        if (affected.Count == 0) return null;

        return new CoherenceIssue(
            "invalid-culture-references",
            "Invalid culture references",
            $"{affected.Count} semantic region(s) reference cultures not defined in the schema. " +
            "Entities placed in these regions will have unresolvable culture assignments.",
            CoherenceIssueSeverity.Warning,
            affected);
    }

    private static Dictionary<string, HashSet<string>> BuildSubtypeIndex(DomainSchema schema)
    {
        var index = new Dictionary<string, HashSet<string>>();
        foreach (var ekd in schema.EntityKinds)
        {
            var subtypes = new HashSet<string>();
            foreach (var s in ekd.Subtypes)
            {
                subtypes.Add(s.Id);
            }
            index[ekd.Kind.Value] = subtypes;
        }
        return index;
    }

    private static Dictionary<string, HashSet<string>> BuildStatusIndex(DomainSchema schema)
    {
        var index = new Dictionary<string, HashSet<string>>();
        foreach (var ekd in schema.EntityKinds)
        {
            var statuses = new HashSet<string>();
            foreach (var s in ekd.Statuses)
            {
                statuses.Add(s.Id);
            }
            index[ekd.Kind.Value] = statuses;
        }
        return index;
    }

    private static void CheckSubtypeSpec(
        string templateId,
        string kind,
        SubtypeSpec spec,
        HashSet<string> validSubtypes,
        List<AffectedItem> affected)
    {
        switch (spec)
        {
            case FixedSubtype fs:
                if (!validSubtypes.Contains(fs.Value))
                {
                    affected.Add(new AffectedItem(
                        templateId,
                        $"{templateId} ({kind})",
                        $"Subtype '{fs.Value}' is not defined for kind '{kind}'"));
                }
                break;

            case RandomSubtype rs:
                foreach (var option in rs.Options)
                {
                    if (!validSubtypes.Contains(option))
                    {
                        affected.Add(new AffectedItem(
                            templateId,
                            $"{templateId} ({kind})",
                            $"Random subtype option '{option}' is not defined for kind '{kind}'"));
                    }
                }
                break;

            case ConditionalSubtype cs:
                if (!string.IsNullOrEmpty(cs.Otherwise) && !validSubtypes.Contains(cs.Otherwise))
                {
                    affected.Add(new AffectedItem(
                        templateId,
                        $"{templateId} ({kind})",
                        $"Conditional otherwise subtype '{cs.Otherwise}' is not defined for kind '{kind}'"));
                }
                foreach (var when in cs.When)
                {
                    if (!validSubtypes.Contains(when.Then))
                    {
                        affected.Add(new AffectedItem(
                            templateId,
                            $"{templateId} ({kind})",
                            $"Conditional subtype '{when.Then}' is not defined for kind '{kind}'"));
                    }
                }
                break;

            // InheritSubtype and FromPressureSubtype are resolved at runtime
            // and cannot be validated statically.
        }
    }
}
