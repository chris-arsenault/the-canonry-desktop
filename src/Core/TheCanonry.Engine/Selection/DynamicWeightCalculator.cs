namespace TheCanonry.Engine.Selection;

using TheCanonry.Engine.Statistics;

/// <summary>
/// Adjusts template weights based on population deviation from targets.
/// Implements homeostatic control: suppress weights when entity populations are above target,
/// boost weights when below target.
/// </summary>
public class DynamicWeightCalculator
{
    private double _deviationThreshold = 0.2;    // 20% deviation triggers adjustment
    private double _maxSuppressionFactor = 0.8;   // Can suppress down to 20% of base
    private double _maxBoostFactor = 2.0;          // Can boost up to 200% of base

    /// <summary>
    /// Calculate adjusted weight for a template based on population metrics.
    /// </summary>
    /// <param name="templateId">Template identifier.</param>
    /// <param name="baseWeight">Original weight from era configuration.</param>
    /// <param name="metrics">Current population metrics.</param>
    /// <param name="createdKinds">Entity kinds this template creates (kind, subtype pairs).</param>
    public WeightAdjustment CalculateWeight(
        string templateId,
        double baseWeight,
        PopulationMetrics metrics,
        IReadOnlyList<(string Kind, string Subtype)> createdKinds)
    {
        if (baseWeight == 0)
        {
            return new WeightAdjustment
            {
                TemplateId = templateId,
                BaseWeight = baseWeight,
                AdjustedWeight = 0,
                AdjustmentFactor = 0,
                Reason = "Template disabled by era"
            };
        }

        if (createdKinds.Count == 0)
        {
            return new WeightAdjustment
            {
                TemplateId = templateId,
                BaseWeight = baseWeight,
                AdjustedWeight = baseWeight,
                AdjustmentFactor = 1.0,
                Reason = "No tracked entity output"
            };
        }

        var cumulativeAdjustment = 1.0;
        var reasons = new List<string>();

        foreach (var (kind, subtype) in createdKinds)
        {
            var key = $"{kind}:{subtype}";
            if (!metrics.Entities.TryGetValue(key, out var metric) || metric.Target == 0)
                continue;

            var deviation = metric.Deviation;

            if (deviation > _deviationThreshold)
            {
                // Over target: suppress creation
                var suppressionStrength = Math.Min(deviation, _maxSuppressionFactor);
                var suppressionFactor = 1.0 - suppressionStrength;
                cumulativeAdjustment *= suppressionFactor;
                reasons.Add(
                    $"{key} over target by {deviation * 100:F0}% ({metric.Count}/{metric.Target}), " +
                    $"suppressing by {(1.0 - suppressionFactor) * 100:F0}%");
            }
            else if (deviation < -_deviationThreshold)
            {
                // Under target: boost creation
                var boostStrength = Math.Min(Math.Abs(deviation), _maxBoostFactor - 1);
                var boostFactor = 1.0 + boostStrength;
                cumulativeAdjustment *= boostFactor;
                reasons.Add(
                    $"{key} under target by {Math.Abs(deviation) * 100:F0}% ({metric.Count}/{metric.Target}), " +
                    $"boosting by {(boostFactor - 1.0) * 100:F0}%");
            }
        }

        return new WeightAdjustment
        {
            TemplateId = templateId,
            BaseWeight = baseWeight,
            AdjustedWeight = baseWeight * cumulativeAdjustment,
            AdjustmentFactor = cumulativeAdjustment,
            Reason = reasons.Count > 0 ? string.Join("; ", reasons) : "No adjustment needed"
        };
    }

    /// <summary>
    /// Calculate adjusted weights for all templates.
    /// </summary>
    /// <param name="templateWeights">Map of templateId -> base weight.</param>
    /// <param name="metrics">Current population metrics.</param>
    /// <param name="creationInfoMap">Map of templateId -> list of (kind, subtype) pairs created.</param>
    public Dictionary<string, WeightAdjustment> CalculateAllWeights(
        Dictionary<string, double> templateWeights,
        PopulationMetrics metrics,
        Dictionary<string, IReadOnlyList<(string Kind, string Subtype)>> creationInfoMap)
    {
        var adjustments = new Dictionary<string, WeightAdjustment>();

        foreach (var (templateId, baseWeight) in templateWeights)
        {
            var createdKinds = creationInfoMap.TryGetValue(templateId, out var kinds)
                ? kinds
                : Array.Empty<(string, string)>();

            adjustments[templateId] = CalculateWeight(templateId, baseWeight, metrics, createdKinds);
        }

        return adjustments;
    }

    /// <summary>
    /// Get templates that are being suppressed (adjustmentFactor less than 1.0).
    /// </summary>
    public static IReadOnlyList<WeightAdjustment> GetSuppressedTemplates(
        Dictionary<string, WeightAdjustment> adjustments)
    {
        return adjustments.Values
            .Where(adj => adj.AdjustmentFactor < 1.0 && adj.BaseWeight > 0)
            .ToList();
    }

    /// <summary>
    /// Get templates that are being boosted (adjustmentFactor greater than 1.0).
    /// </summary>
    public static IReadOnlyList<WeightAdjustment> GetBoostedTemplates(
        Dictionary<string, WeightAdjustment> adjustments)
    {
        return adjustments.Values
            .Where(adj => adj.AdjustmentFactor > 1.0)
            .ToList();
    }

    /// <summary>
    /// Configure adjustment parameters.
    /// </summary>
    public void Configure(double deviationThreshold, double maxSuppressionFactor, double maxBoostFactor)
    {
        _deviationThreshold = deviationThreshold;
        _maxSuppressionFactor = maxSuppressionFactor;
        _maxBoostFactor = maxBoostFactor;
    }
}
