using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules;
using TheCanonry.Engine.Rules.Types;

using static TheCanonry.Engine.Tests.Rules.TestHelpers;

namespace TheCanonry.Engine.Tests.Rules;

public class MetricEvaluatorTests
{
    // =========================================================================
    // ENTITY COUNT METRIC
    // =========================================================================

    [Fact]
    public void EntityCountMetric_CountsMatchingEntities()
    {
        var graph = CreateGraphWithEntities(
            CreateTestEntity("f1", kind: "faction"),
            CreateTestEntity("f2", kind: "faction"),
            CreateTestEntity("n1", kind: "npc"));
        var ctx = CreateSystemContext(graph);

        var metric = new EntityCountMetric("faction", "", "", Coefficient: 1.0, Cap: 0);
        var result = MetricEvaluator.Evaluate(metric, ctx);

        Assert.Equal(2.0, result);
    }

    [Fact]
    public void EntityCountMetric_AppliesCoefficient()
    {
        var graph = CreateGraphWithEntities(
            CreateTestEntity("f1", kind: "faction"),
            CreateTestEntity("f2", kind: "faction"));
        var ctx = CreateSystemContext(graph);

        var metric = new EntityCountMetric("faction", "", "", Coefficient: 0.5, Cap: 0);
        var result = MetricEvaluator.Evaluate(metric, ctx);

        Assert.Equal(1.0, result);
    }

    [Fact]
    public void EntityCountMetric_AppliesCap()
    {
        var graph = CreateGraphWithEntities(
            CreateTestEntity("f1", kind: "faction"),
            CreateTestEntity("f2", kind: "faction"),
            CreateTestEntity("f3", kind: "faction"),
            CreateTestEntity("f4", kind: "faction"),
            CreateTestEntity("f5", kind: "faction"));
        var ctx = CreateSystemContext(graph);

        var metric = new EntityCountMetric("faction", "", "", Coefficient: 1.0, Cap: 3.0);
        var result = MetricEvaluator.Evaluate(metric, ctx);

        Assert.Equal(3.0, result);
    }

    // =========================================================================
    // TOTAL ENTITIES METRIC
    // =========================================================================

    [Fact]
    public void TotalEntitiesMetric_CountsAllEntities()
    {
        var graph = CreateGraphWithEntities(
            CreateTestEntity("f1", kind: "faction"),
            CreateTestEntity("n1", kind: "npc"),
            CreateTestEntity("l1", kind: "location"));
        var ctx = CreateSystemContext(graph);

        var metric = new TotalEntitiesMetric(Coefficient: 1.0, Cap: 0);
        var result = MetricEvaluator.Evaluate(metric, ctx);

        Assert.Equal(3.0, result);
    }

    // =========================================================================
    // CONSTANT METRIC
    // =========================================================================

    [Fact]
    public void ConstantMetric_ReturnsValueTimesCoefficient()
    {
        var graph = new GraphStore();
        var ctx = CreateSystemContext(graph);

        var metric = new ConstantMetric(Value: 5.0, Coefficient: 2.0);
        var result = MetricEvaluator.Evaluate(metric, ctx);

        Assert.Equal(10.0, result);
    }

    // =========================================================================
    // RATIO METRIC
    // =========================================================================

    [Fact]
    public void RatioMetric_CalculatesCorrectRatio()
    {
        var graph = CreateGraphWithEntities(
            CreateTestEntity("f1", kind: "faction", status: "active"),
            CreateTestEntity("f2", kind: "faction", status: "active"),
            CreateTestEntity("n1", kind: "npc"));
        var ctx = CreateSystemContext(graph);

        var metric = new RatioMetric(
            Numerator: new SimpleEntityCount("faction", "", ""),
            Denominator: new SimpleTotalEntities(),
            FallbackValue: 0.0,
            Coefficient: 1.0,
            Cap: 0);

        var result = MetricEvaluator.Evaluate(metric, ctx);
        Assert.Equal(2.0 / 3.0, result, 3);
    }

    [Fact]
    public void RatioMetric_ReturnsFallbackOnZeroDenominator()
    {
        var graph = new GraphStore(); // empty
        var ctx = CreateSystemContext(graph);

        var metric = new RatioMetric(
            Numerator: new SimpleEntityCount("faction", "", ""),
            Denominator: new SimpleTotalEntities(),
            FallbackValue: -1.0,
            Coefficient: 1.0,
            Cap: 0);

        var result = MetricEvaluator.Evaluate(metric, ctx);
        Assert.Equal(-1.0, result);
    }

    // =========================================================================
    // DECAY RATE METRIC
    // =========================================================================

    [Fact]
    public void DecayRateMetric_ReturnsCorrectValues()
    {
        var graph = new GraphStore();
        var ctx = CreateSystemContext(graph);

        Assert.Equal(0.0, MetricEvaluator.Evaluate(new DecayRateMetric("none"), ctx));
        Assert.Equal(0.01, MetricEvaluator.Evaluate(new DecayRateMetric("slow"), ctx));
        Assert.Equal(0.05, MetricEvaluator.Evaluate(new DecayRateMetric("medium"), ctx));
        Assert.Equal(0.1, MetricEvaluator.Evaluate(new DecayRateMetric("fast"), ctx));
    }

    // =========================================================================
    // FALLOFF METRIC
    // =========================================================================

    [Fact]
    public void FalloffMetric_LinearFalloff()
    {
        var graph = new GraphStore();
        var ctx = CreateSystemContext(graph);

        // At distance 0, falloff should be 1.0
        Assert.Equal(1.0, MetricEvaluator.Evaluate(
            new FalloffMetric("linear", Distance: 0.0, MaxDistance: 10.0), ctx));

        // At half distance, falloff should be 0.5
        Assert.Equal(0.5, MetricEvaluator.Evaluate(
            new FalloffMetric("linear", Distance: 5.0, MaxDistance: 10.0), ctx));

        // At max distance, falloff should be 0.0
        Assert.Equal(0.0, MetricEvaluator.Evaluate(
            new FalloffMetric("linear", Distance: 10.0, MaxDistance: 10.0), ctx));
    }

    [Fact]
    public void FalloffMetric_NoneFalloff_AlwaysReturnsOne()
    {
        var graph = new GraphStore();
        var ctx = CreateSystemContext(graph);

        Assert.Equal(1.0, MetricEvaluator.Evaluate(
            new FalloffMetric("none", Distance: 50.0, MaxDistance: 10.0), ctx));
    }

    // =========================================================================
    // PROMINENCE MULTIPLIER METRIC
    // =========================================================================

    [Fact]
    public void ProminenceMultiplierMetric_ReturnsCorrectMultiplier()
    {
        var entity = CreateTestEntity("f1", prominence: 2.5); // recognized = index 2
        var graph = CreateGraphWithEntities(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        // success_chance: index 2 => 1.0
        Assert.Equal(1.0, MetricEvaluator.Evaluate(
            new ProminenceMultiplierMetric("success_chance"), ctx));

        // action_rate: index 2 => 1.0
        Assert.Equal(1.0, MetricEvaluator.Evaluate(
            new ProminenceMultiplierMetric("action_rate"), ctx));
    }

    [Fact]
    public void ProminenceMultiplierMetric_HighProminenceGivesBonus()
    {
        var entity = CreateTestEntity("f1", prominence: 4.5); // mythic = index 4
        var graph = CreateGraphWithEntities(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        // success_chance: index 4 => 1.5
        Assert.Equal(1.5, MetricEvaluator.Evaluate(
            new ProminenceMultiplierMetric("success_chance"), ctx));

        // action_rate: index 4 => 2.0
        Assert.Equal(2.0, MetricEvaluator.Evaluate(
            new ProminenceMultiplierMetric("action_rate"), ctx));
    }
}
