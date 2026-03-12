namespace TheCanonry.Engine.Tests.Rules;

using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;

using static TestHelpers;

public class ConditionEvaluatorTests
{
    // =========================================================================
    // PRESSURE CONDITIONS
    // =========================================================================

    [Fact]
    public void PressureCondition_PassesWhenInRange()
    {
        var graph = new GraphStore();
        graph.Pressures["conflict"] = 0.5;
        var ctx = CreateSystemContext(graph);

        var condition = new PressureCondition("conflict", Min: 0.0, Max: 1.0);
        Assert.True(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void PressureCondition_FailsWhenBelowMin()
    {
        var graph = new GraphStore();
        graph.Pressures["conflict"] = 0.1;
        var ctx = CreateSystemContext(graph);

        var condition = new PressureCondition("conflict", Min: 0.5, Max: 1.0);
        Assert.False(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void PressureCondition_FailsWhenAboveMax()
    {
        var graph = new GraphStore();
        graph.Pressures["conflict"] = 1.5;
        var ctx = CreateSystemContext(graph);

        var condition = new PressureCondition("conflict", Min: 0.0, Max: 1.0);
        Assert.False(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void PressureCompareCondition_GreaterThan()
    {
        var graph = new GraphStore();
        graph.Pressures["conflict"] = 0.8;
        graph.Pressures["prosperity"] = 0.3;
        var ctx = CreateSystemContext(graph);

        var condition = new PressureCompareCondition("conflict", "prosperity", ComparisonOperator.GreaterThan);
        Assert.True(ConditionEvaluator.Evaluate(condition, ctx));

        var reverse = new PressureCompareCondition("prosperity", "conflict", ComparisonOperator.GreaterThan);
        Assert.False(ConditionEvaluator.Evaluate(reverse, ctx));
    }

    [Fact]
    public void PressureCompareCondition_LessThanOrEqual()
    {
        var graph = new GraphStore();
        graph.Pressures["a"] = 0.5;
        graph.Pressures["b"] = 0.5;
        var ctx = CreateSystemContext(graph);

        var condition = new PressureCompareCondition("a", "b", ComparisonOperator.LessThanOrEqual);
        Assert.True(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void PressureAnyAboveCondition_Passes()
    {
        var graph = new GraphStore();
        graph.Pressures["a"] = 0.2;
        graph.Pressures["b"] = 0.9;
        var ctx = CreateSystemContext(graph);

        var condition = new PressureAnyAboveCondition(["a", "b"], Threshold: 0.5);
        Assert.True(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void PressureAnyAboveCondition_FailsWhenAllBelow()
    {
        var graph = new GraphStore();
        graph.Pressures["a"] = 0.2;
        graph.Pressures["b"] = 0.3;
        var ctx = CreateSystemContext(graph);

        var condition = new PressureAnyAboveCondition(["a", "b"], Threshold: 0.5);
        Assert.False(ConditionEvaluator.Evaluate(condition, ctx));
    }

    // =========================================================================
    // ENTITY COUNT
    // =========================================================================

    [Fact]
    public void EntityCountCondition_PassesWhenInRange()
    {
        var graph = CreateGraphWithEntities(
            CreateTestEntity("f1", kind: "faction"),
            CreateTestEntity("f2", kind: "faction"),
            CreateTestEntity("n1", kind: "npc"));
        var ctx = CreateSystemContext(graph);

        var condition = new EntityCountCondition("faction", "", "", Min: 1, Max: 5, OvershootFactor: 1.5);
        Assert.True(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void EntityCountCondition_FailsWhenBelowMin()
    {
        var graph = CreateGraphWithEntities(
            CreateTestEntity("f1", kind: "faction"));
        var ctx = CreateSystemContext(graph);

        var condition = new EntityCountCondition("faction", "", "", Min: 5, Max: 10, OvershootFactor: 1.5);
        Assert.False(ConditionEvaluator.Evaluate(condition, ctx));
    }

    // =========================================================================
    // AND/OR COMPOSITION
    // =========================================================================

    [Fact]
    public void AndCondition_PassesWhenAllPass()
    {
        var graph = new GraphStore();
        graph.Pressures["a"] = 0.5;
        graph.Pressures["b"] = 0.8;
        var ctx = CreateSystemContext(graph);

        var condition = new AndCondition([
            new PressureCondition("a", Min: 0.0, Max: 1.0),
            new PressureCondition("b", Min: 0.5, Max: 1.0)
        ]);

        Assert.True(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void AndCondition_FailsWhenOneFails()
    {
        var graph = new GraphStore();
        graph.Pressures["a"] = 0.5;
        graph.Pressures["b"] = 0.1;
        var ctx = CreateSystemContext(graph);

        var condition = new AndCondition([
            new PressureCondition("a", Min: 0.0, Max: 1.0),
            new PressureCondition("b", Min: 0.5, Max: 1.0)
        ]);

        Assert.False(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void OrCondition_PassesWhenOnePasses()
    {
        var graph = new GraphStore();
        graph.Pressures["a"] = 0.1;
        graph.Pressures["b"] = 0.8;
        var ctx = CreateSystemContext(graph);

        var condition = new OrCondition([
            new PressureCondition("a", Min: 0.5, Max: 1.0),
            new PressureCondition("b", Min: 0.5, Max: 1.0)
        ]);

        Assert.True(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void OrCondition_FailsWhenAllFail()
    {
        var graph = new GraphStore();
        graph.Pressures["a"] = 0.1;
        graph.Pressures["b"] = 0.2;
        var ctx = CreateSystemContext(graph);

        var condition = new OrCondition([
            new PressureCondition("a", Min: 0.5, Max: 1.0),
            new PressureCondition("b", Min: 0.5, Max: 1.0)
        ]);

        Assert.False(ConditionEvaluator.Evaluate(condition, ctx));
    }

    // =========================================================================
    // RANDOM CHANCE
    // =========================================================================

    [Fact]
    public void RandomChanceCondition_AlwaysPassesWithChance1()
    {
        var graph = new GraphStore();
        var ctx = CreateSystemContext(graph);

        var condition = new RandomChanceCondition(Chance: 1.0);

        // With chance 1.0, should always pass
        for (var i = 0; i < 100; i++)
            Assert.True(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void RandomChanceCondition_NeverPassesWithChance0()
    {
        var graph = new GraphStore();
        var ctx = CreateSystemContext(graph);

        var condition = new RandomChanceCondition(Chance: 0.0);

        // With chance 0.0, should never pass
        for (var i = 0; i < 100; i++)
            Assert.False(ConditionEvaluator.Evaluate(condition, ctx));
    }

    // =========================================================================
    // ERA MATCH
    // =========================================================================

    [Fact]
    public void EraMatchCondition_PassesWhenCurrentEraMatches()
    {
        var graph = new GraphStore();
        var ctx = CreateSystemContext(graph, currentEraId: "era-2");

        var condition = new EraMatchCondition(["era-1", "era-2"]);
        Assert.True(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void EraMatchCondition_FailsWhenCurrentEraDoesNotMatch()
    {
        var graph = new GraphStore();
        var ctx = CreateSystemContext(graph, currentEraId: "era-3");

        var condition = new EraMatchCondition(["era-1", "era-2"]);
        Assert.False(ConditionEvaluator.Evaluate(condition, ctx));
    }

    // =========================================================================
    // STATUS CONDITION
    // =========================================================================

    [Fact]
    public void StatusCondition_MatchesEntityStatus()
    {
        var graph = new GraphStore();
        var entity = CreateTestEntity("f1", status: "active");
        graph.CreateEntity(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        Assert.True(ConditionEvaluator.Evaluate(new StatusCondition("active", Not: false), ctx));
        Assert.False(ConditionEvaluator.Evaluate(new StatusCondition("historical", Not: false), ctx));
    }

    [Fact]
    public void StatusCondition_NegatedMatchesEntityStatus()
    {
        var graph = new GraphStore();
        var entity = CreateTestEntity("f1", status: "active");
        graph.CreateEntity(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        Assert.False(ConditionEvaluator.Evaluate(new StatusCondition("active", Not: true), ctx));
        Assert.True(ConditionEvaluator.Evaluate(new StatusCondition("historical", Not: true), ctx));
    }

    // =========================================================================
    // TAG CONDITIONS
    // =========================================================================

    [Fact]
    public void TagExistsCondition_MatchesExistingTag()
    {
        var graph = new GraphStore();
        var entity = CreateTestEntity("f1");
        entity.Tags.Set("warlike", true);
        graph.CreateEntity(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        var condition = new TagExistsCondition("", "warlike", "");
        Assert.True(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void LacksTagCondition_PassesWhenTagAbsent()
    {
        var graph = new GraphStore();
        var entity = CreateTestEntity("f1");
        graph.CreateEntity(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        var condition = new LacksTagCondition("", "warlike");
        Assert.True(ConditionEvaluator.Evaluate(condition, ctx));
    }

    [Fact]
    public void LacksTagCondition_FailsWhenTagPresent()
    {
        var graph = new GraphStore();
        var entity = CreateTestEntity("f1");
        entity.Tags.Set("warlike", true);
        graph.CreateEntity(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        var condition = new LacksTagCondition("", "warlike");
        Assert.False(ConditionEvaluator.Evaluate(condition, ctx));
    }

    // =========================================================================
    // ALWAYS CONDITION
    // =========================================================================

    [Fact]
    public void AlwaysCondition_AlwaysPasses()
    {
        var graph = new GraphStore();
        var ctx = CreateSystemContext(graph);

        Assert.True(ConditionEvaluator.Evaluate(new AlwaysCondition(), ctx));
    }

    // =========================================================================
    // PROMINENCE CONDITION
    // =========================================================================

    [Fact]
    public void ProminenceCondition_MatchesRange()
    {
        var graph = new GraphStore();
        var entity = CreateTestEntity("f1", prominence: 2.5); // "recognized" range
        graph.CreateEntity(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        // Recognized is 2.0-2.99, should pass for [recognized, renowned]
        Assert.True(ConditionEvaluator.Evaluate(
            new ProminenceCondition("recognized", "renowned"), ctx));

        // Should fail for [mythic, mythic] since 2.5 < 4.0
        Assert.False(ConditionEvaluator.Evaluate(
            new ProminenceCondition("mythic", "mythic"), ctx));
    }
}
