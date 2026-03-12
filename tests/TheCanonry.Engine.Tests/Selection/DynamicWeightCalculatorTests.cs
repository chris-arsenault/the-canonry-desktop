namespace TheCanonry.Engine.Tests.Selection;

using TheCanonry.Engine.Selection;
using TheCanonry.Engine.Statistics;

public class DynamicWeightCalculatorTests
{
    private static PopulationMetrics CreateMetrics(
        params (string Key, string Kind, string Subtype, int Count, int Target)[] entries)
    {
        var metrics = new PopulationMetrics();
        foreach (var (key, kind, subtype, count, target) in entries)
        {
            metrics.Entities[key] = new EntityMetric
            {
                Kind = kind,
                Subtype = subtype,
                Count = count,
                Target = target,
                Deviation = target > 0 ? (double)(count - target) / target : 0
            };
        }
        return metrics;
    }

    [Fact]
    public void CalculateWeight_OverpopulatedKind_SuppressesWeight()
    {
        var calculator = new DynamicWeightCalculator();
        // 15/10 = deviation 0.5, which exceeds default threshold of 0.2
        var metrics = CreateMetrics(("faction:merchant", "faction", "merchant", 15, 10));

        var result = calculator.CalculateWeight(
            "tmpl-1", 1.0, metrics,
            [("faction", "merchant")]);

        Assert.True(result.AdjustedWeight < result.BaseWeight,
            $"Expected suppressed weight, got {result.AdjustedWeight}");
        Assert.True(result.AdjustmentFactor < 1.0);
        Assert.Contains("over target", result.Reason);
    }

    [Fact]
    public void CalculateWeight_UnderpopulatedKind_BoostsWeight()
    {
        var calculator = new DynamicWeightCalculator();
        // 3/10 = deviation -0.7, below -0.2 threshold
        var metrics = CreateMetrics(("faction:merchant", "faction", "merchant", 3, 10));

        var result = calculator.CalculateWeight(
            "tmpl-1", 1.0, metrics,
            [("faction", "merchant")]);

        Assert.True(result.AdjustedWeight > result.BaseWeight,
            $"Expected boosted weight, got {result.AdjustedWeight}");
        Assert.True(result.AdjustmentFactor > 1.0);
        Assert.Contains("under target", result.Reason);
    }

    [Fact]
    public void CalculateWeight_AtTarget_ReturnsBaseWeight()
    {
        var calculator = new DynamicWeightCalculator();
        // 10/10 = deviation 0.0, within threshold
        var metrics = CreateMetrics(("faction:merchant", "faction", "merchant", 10, 10));

        var result = calculator.CalculateWeight(
            "tmpl-1", 1.0, metrics,
            [("faction", "merchant")]);

        Assert.Equal(1.0, result.AdjustedWeight, precision: 5);
        Assert.Equal(1.0, result.AdjustmentFactor, precision: 5);
        Assert.Equal("No adjustment needed", result.Reason);
    }

    [Fact]
    public void CalculateWeight_ZeroBaseWeight_ReturnsDisabled()
    {
        var calculator = new DynamicWeightCalculator();
        var metrics = CreateMetrics(("faction:merchant", "faction", "merchant", 3, 10));

        var result = calculator.CalculateWeight(
            "tmpl-1", 0.0, metrics,
            [("faction", "merchant")]);

        Assert.Equal(0.0, result.AdjustedWeight);
        Assert.Equal(0.0, result.AdjustmentFactor);
        Assert.Equal("Template disabled by era", result.Reason);
    }

    [Fact]
    public void CalculateWeight_NoCreatedKinds_ReturnsBaseWeight()
    {
        var calculator = new DynamicWeightCalculator();
        var metrics = CreateMetrics(("faction:merchant", "faction", "merchant", 3, 10));

        var result = calculator.CalculateWeight(
            "tmpl-1", 1.0, metrics,
            Array.Empty<(string, string)>());

        Assert.Equal(1.0, result.AdjustedWeight, precision: 5);
        Assert.Equal("No tracked entity output", result.Reason);
    }

    [Fact]
    public void CalculateWeight_MultipleCreatedKinds_CumulativeAdjustment()
    {
        var calculator = new DynamicWeightCalculator();
        // Both kinds are underpopulated
        var metrics = CreateMetrics(
            ("faction:merchant", "faction", "merchant", 3, 10),
            ("npc:warrior", "npc", "warrior", 2, 10));

        var result = calculator.CalculateWeight(
            "tmpl-1", 1.0, metrics,
            [("faction", "merchant"), ("npc", "warrior")]);

        // Both should boost, so cumulative should be higher than either alone
        Assert.True(result.AdjustmentFactor > 1.0);
        Assert.True(result.AdjustedWeight > 1.0);
    }

    [Fact]
    public void Configure_ChangesThresholds()
    {
        var calculator = new DynamicWeightCalculator();
        // With default threshold 0.2, a deviation of 0.15 should NOT trigger
        var metrics = CreateMetrics(("faction:merchant", "faction", "merchant", 11, 10)); // deviation = 0.1

        var baseResult = calculator.CalculateWeight(
            "tmpl-1", 1.0, metrics,
            [("faction", "merchant")]);
        Assert.Equal(1.0, baseResult.AdjustmentFactor, precision: 5);

        // Now lower threshold to 0.05 so deviation 0.1 triggers suppression
        calculator.Configure(deviationThreshold: 0.05, maxSuppressionFactor: 0.8, maxBoostFactor: 2.0);

        var adjustedResult = calculator.CalculateWeight(
            "tmpl-1", 1.0, metrics,
            [("faction", "merchant")]);
        Assert.True(adjustedResult.AdjustmentFactor < 1.0);
    }

    [Fact]
    public void CalculateAllWeights_ProcessesAllTemplates()
    {
        var calculator = new DynamicWeightCalculator();
        var metrics = CreateMetrics(
            ("faction:merchant", "faction", "merchant", 15, 10),
            ("npc:warrior", "npc", "warrior", 3, 10));

        var templateWeights = new Dictionary<string, double>
        {
            ["tmpl-factions"] = 1.0,
            ["tmpl-npcs"] = 1.0
        };

        var creationInfo = new Dictionary<string, IReadOnlyList<(string Kind, string Subtype)>>
        {
            ["tmpl-factions"] = [("faction", "merchant")],
            ["tmpl-npcs"] = [("npc", "warrior")]
        };

        var adjustments = calculator.CalculateAllWeights(templateWeights, metrics, creationInfo);

        Assert.Equal(2, adjustments.Count);
        // Factions are overpopulated => suppressed
        Assert.True(adjustments["tmpl-factions"].AdjustmentFactor < 1.0);
        // NPCs are underpopulated => boosted
        Assert.True(adjustments["tmpl-npcs"].AdjustmentFactor > 1.0);
    }

    [Fact]
    public void GetSuppressedAndBoostedTemplates_FiltersCorrectly()
    {
        var calculator = new DynamicWeightCalculator();
        var metrics = CreateMetrics(
            ("faction:merchant", "faction", "merchant", 15, 10),
            ("npc:warrior", "npc", "warrior", 3, 10));

        var templateWeights = new Dictionary<string, double>
        {
            ["tmpl-factions"] = 1.0,
            ["tmpl-npcs"] = 1.0,
            ["tmpl-disabled"] = 0.0
        };

        var creationInfo = new Dictionary<string, IReadOnlyList<(string Kind, string Subtype)>>
        {
            ["tmpl-factions"] = [("faction", "merchant")],
            ["tmpl-npcs"] = [("npc", "warrior")],
            ["tmpl-disabled"] = [("faction", "merchant")]
        };

        var adjustments = calculator.CalculateAllWeights(templateWeights, metrics, creationInfo);

        var suppressed = DynamicWeightCalculator.GetSuppressedTemplates(adjustments);
        var boosted = DynamicWeightCalculator.GetBoostedTemplates(adjustments);

        Assert.Single(suppressed);
        Assert.Equal("tmpl-factions", suppressed[0].TemplateId);
        Assert.Single(boosted);
        Assert.Equal("tmpl-npcs", boosted[0].TemplateId);
    }

    [Fact]
    public void CalculateWeight_UnknownKindInMetrics_SkipsGracefully()
    {
        var calculator = new DynamicWeightCalculator();
        // Metrics has no entry for the kind this template creates
        var metrics = new PopulationMetrics();

        var result = calculator.CalculateWeight(
            "tmpl-1", 1.0, metrics,
            [("faction", "merchant")]);

        // No metric found, no adjustment
        Assert.Equal(1.0, result.AdjustedWeight, precision: 5);
        Assert.Equal("No adjustment needed", result.Reason);
    }
}
