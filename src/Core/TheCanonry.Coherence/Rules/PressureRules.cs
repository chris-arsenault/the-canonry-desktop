namespace TheCanonry.Coherence.Rules;

using TheCanonry.Coherence.Types;
using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Templates;

/// <summary>
/// Rules that check pressure source/sink completeness.
/// A pressure without sources will never rise; a pressure without sinks will never fall.
/// Both conditions indicate a misconfigured feedback loop.
/// </summary>
public static class PressureRules
{
    /// <summary>
    /// Detects pressures with no source of positive delta.
    /// A pressure has a source if any template's StateUpdates contains a
    /// ModifyPressureMutation with matching PressureId and Delta > 0,
    /// or if the pressure's Growth.PositiveFeedback has entries.
    /// </summary>
    public static CoherenceIssue? PressureWithoutSources(EngineConfig config)
    {
        var templateSourceIds = new HashSet<string>();

        foreach (var template in config.Templates)
        {
            CollectPressureSources(template.StateUpdates, templateSourceIds);
            foreach (var variant in template.Variants.Options)
            {
                CollectPressureSources(variant.Apply.StateUpdates, templateSourceIds);
            }
        }

        var affected = new List<AffectedItem>();

        foreach (var pressure in config.Pressures)
        {
            var hasTemplateSources = templateSourceIds.Contains(pressure.Id);
            var hasFeedbackSources = pressure.Growth.PositiveFeedback.Count > 0;

            if (!hasTemplateSources && !hasFeedbackSources)
            {
                affected.Add(new AffectedItem(
                    pressure.Id,
                    pressure.Name,
                    "No template mutations or positive feedback drive this pressure upward"));
            }
        }

        if (affected.Count == 0) return null;

        return new CoherenceIssue(
            "pressure-without-sources",
            "Pressures without sources",
            $"{affected.Count} pressure(s) have no source of positive delta. " +
            "They will never rise above their initial value, making them inert.",
            CoherenceIssueSeverity.Warning,
            affected);
    }

    /// <summary>
    /// Detects pressures with no sink for negative delta.
    /// A pressure has a sink if any template's StateUpdates contains a
    /// ModifyPressureMutation with matching PressureId and Delta &lt; 0,
    /// or if the pressure's Homeostasis > 0,
    /// or if the pressure's Growth.NegativeFeedback has entries.
    /// </summary>
    public static CoherenceIssue? PressureWithoutSinks(EngineConfig config)
    {
        var templateSinkIds = new HashSet<string>();

        foreach (var template in config.Templates)
        {
            CollectPressureSinks(template.StateUpdates, templateSinkIds);
            foreach (var variant in template.Variants.Options)
            {
                CollectPressureSinks(variant.Apply.StateUpdates, templateSinkIds);
            }
        }

        var affected = new List<AffectedItem>();

        foreach (var pressure in config.Pressures)
        {
            var hasTemplateSinks = templateSinkIds.Contains(pressure.Id);
            var hasHomeostasis = pressure.Homeostasis > 0;
            var hasFeedbackSinks = pressure.Growth.NegativeFeedback.Count > 0;

            if (!hasTemplateSinks && !hasHomeostasis && !hasFeedbackSinks)
            {
                affected.Add(new AffectedItem(
                    pressure.Id,
                    pressure.Name,
                    "No template mutations, homeostasis, or negative feedback reduce this pressure"));
            }
        }

        if (affected.Count == 0) return null;

        return new CoherenceIssue(
            "pressure-without-sinks",
            "Pressures without sinks",
            $"{affected.Count} pressure(s) have no mechanism to decrease. " +
            "They will accumulate indefinitely once any source pushes them upward.",
            CoherenceIssueSeverity.Warning,
            affected);
    }

    private static void CollectPressureSources(
        IReadOnlyList<Mutation> mutations, HashSet<string> sourceIds)
    {
        foreach (var mutation in mutations)
        {
            if (mutation is ModifyPressureMutation { Delta: > 0 } m)
            {
                sourceIds.Add(m.PressureId);
            }
        }
    }

    private static void CollectPressureSinks(
        IReadOnlyList<Mutation> mutations, HashSet<string> sinkIds)
    {
        foreach (var mutation in mutations)
        {
            if (mutation is ModifyPressureMutation { Delta: < 0 } m)
            {
                sinkIds.Add(m.PressureId);
            }
        }
    }
}
