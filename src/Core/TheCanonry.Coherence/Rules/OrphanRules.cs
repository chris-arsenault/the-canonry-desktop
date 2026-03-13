using TheCanonry.Coherence.Types;
using TheCanonry.Engine.Engine;

namespace TheCanonry.Coherence.Rules;

/// <summary>
/// Rules that detect orphaned configuration elements -- templates and systems
/// that are defined but never referenced from any era, making them dead config.
/// </summary>
public static class OrphanRules
{
    /// <summary>
    /// Detects enabled templates not referenced in any era's TemplateWeights.
    /// These templates will never be selected during generation.
    /// </summary>
    public static CoherenceIssue? OrphanGenerators(EngineConfig config)
    {
        var referencedIds = new HashSet<string>();
        foreach (var era in config.Eras)
        {
            foreach (var key in era.TemplateWeights.Keys)
            {
                referencedIds.Add(key);
            }
        }

        var affected = new List<AffectedItem>();

        foreach (var template in config.Templates)
        {
            if (!template.Enabled) continue;

            if (!referencedIds.Contains(template.Id))
            {
                affected.Add(new AffectedItem(
                    template.Id,
                    template.Name,
                    "Not referenced in any era's template weights"));
            }
        }

        if (affected.Count == 0) return null;

        return new CoherenceIssue(
            "orphan-generators",
            "Orphaned templates",
            $"{affected.Count} enabled template(s) are not referenced in any era's TemplateWeights. " +
            "They will never run during generation.",
            CoherenceIssueSeverity.Warning,
            affected);
    }

    /// <summary>
    /// Detects enabled systems whose id is not referenced in any era's SystemModifiers.
    /// These systems will run at their default weight but are invisible to era tuning.
    /// </summary>
    public static CoherenceIssue? OrphanSystems(EngineConfig config)
    {
        var referencedIds = new HashSet<string>();
        foreach (var era in config.Eras)
        {
            foreach (var key in era.SystemModifiers.Keys)
            {
                referencedIds.Add(key);
            }
        }

        var affected = new List<AffectedItem>();

        foreach (var system in config.Systems)
        {
            if (!system.Enabled) continue;

            if (!system.Config.TryGetValue("id", out var idObj) || idObj is not string systemId)
                continue;

            if (!referencedIds.Contains(systemId))
            {
                affected.Add(new AffectedItem(
                    systemId,
                    $"{system.SystemType} system",
                    "Not referenced in any era's system modifiers"));
            }
        }

        if (affected.Count == 0) return null;

        return new CoherenceIssue(
            "orphan-systems",
            "Orphaned systems",
            $"{affected.Count} enabled system(s) are not referenced in any era's SystemModifiers. " +
            "They will run but cannot be tuned per era.",
            CoherenceIssueSeverity.Warning,
            affected);
    }

    /// <summary>
    /// Detects enabled templates that are referenced in era TemplateWeights
    /// but have weight 0 in every era where they appear.
    /// </summary>
    public static CoherenceIssue? ZeroWeightGenerators(EngineConfig config)
    {
        // Build a map: templateId -> list of weights across eras
        var templateWeights = new Dictionary<string, List<double>>();
        foreach (var era in config.Eras)
        {
            foreach (var (id, weight) in era.TemplateWeights)
            {
                if (!templateWeights.TryGetValue(id, out var weights))
                {
                    weights = [];
                    templateWeights[id] = weights;
                }
                weights.Add(weight);
            }
        }

        var enabledIds = new HashSet<string>();
        foreach (var template in config.Templates)
        {
            if (template.Enabled)
                enabledIds.Add(template.Id);
        }

        var affected = new List<AffectedItem>();

        foreach (var (id, weights) in templateWeights)
        {
            if (!enabledIds.Contains(id)) continue;

            // Only flag if it IS referenced but always with weight 0
            var allZero = weights.TrueForAll(w => w == 0);
            if (allZero)
            {
                affected.Add(new AffectedItem(
                    id,
                    id,
                    $"Referenced in {weights.Count} era(s) but with weight 0 in all of them"));
            }
        }

        if (affected.Count == 0) return null;

        return new CoherenceIssue(
            "zero-weight-generators",
            "Zero-weight templates",
            $"{affected.Count} enabled template(s) are referenced in era weights but have weight 0 " +
            "in every era. They will never be selected.",
            CoherenceIssueSeverity.Warning,
            affected);
    }
}
