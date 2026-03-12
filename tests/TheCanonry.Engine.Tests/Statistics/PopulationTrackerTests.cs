namespace TheCanonry.Engine.Tests.Statistics;

using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Statistics;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

public class PopulationTrackerTests
{
    private static Entity CreateEntity(
        string id, string kind, string subtype,
        string culture = "northern", double prominence = 2.0, int tick = 1)
    {
        var entity = new Entity(
            id: new EntityId(id),
            kind: new EntityKind(kind),
            subtype: subtype,
            name: $"Test {id}",
            culture: new CultureId(culture),
            eraId: new EraId("era-1"),
            coordinates: new SemanticCoordinates(0.5, 0.3, 0.1),
            createdBy: new ExecutionContext(tick, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: tick);

        if (Math.Abs(prominence - 0.0) > 1e-9)
            entity.SetProminence(new Prominence(prominence), tick);

        return entity;
    }

    private static Relationship CreateRelationship(
        string srcId, string dstId, string kind = "allied_with", int tick = 1)
    {
        return new Relationship(
            sourceId: new EntityId(srcId),
            targetId: new EntityId(dstId),
            kind: new RelationshipKind(kind),
            strength: 0.5,
            distance: 0.0,
            category: "",
            createdBy: new ExecutionContext(tick, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: tick);
    }

    private static GraphStore CreateEmptyGraph() => new();

    private static DistributionTargets CreateTargets(params (string Kind, string Subtype, int Target)[] entries)
    {
        var targets = new DistributionTargets();
        foreach (var (kind, subtype, target) in entries)
        {
            if (!targets.Entities.ContainsKey(kind))
                targets.Entities[kind] = [];
            targets.Entities[kind][subtype] = target;
        }
        return targets;
    }

    // =========================================================================
    // PopulationTracker Tests
    // =========================================================================

    [Fact]
    public void Update_EmptyGraph_AllCountsAreZero()
    {
        var targets = CreateTargets(("faction", "merchant", 5), ("npc", "warrior", 10));
        var tracker = new PopulationTracker(targets);

        tracker.Update(CreateEmptyGraph());

        var metrics = tracker.GetMetrics();
        Assert.Equal(0, metrics.Entities["faction:merchant"].Count);
        Assert.Equal(0, metrics.Entities["npc:warrior"].Count);
    }

    [Fact]
    public void Update_WithEntities_CountsMatchGraph()
    {
        var targets = CreateTargets(("faction", "merchant", 5));
        var tracker = new PopulationTracker(targets);
        var graph = new GraphStore();
        graph.CreateEntity(CreateEntity("f1", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("f2", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("f3", "faction", "merchant"));

        tracker.Update(graph);

        var metrics = tracker.GetMetrics();
        Assert.Equal(3, metrics.Entities["faction:merchant"].Count);
        Assert.Equal(5, metrics.Entities["faction:merchant"].Target);
    }

    [Fact]
    public void Update_DeviationCalculation_OverTarget()
    {
        var targets = CreateTargets(("faction", "merchant", 5));
        var tracker = new PopulationTracker(targets);
        var graph = new GraphStore();
        // Create 8 entities, target is 5 => deviation = (8-5)/5 = 0.6
        for (var i = 0; i < 8; i++)
            graph.CreateEntity(CreateEntity($"f{i}", "faction", "merchant"));

        tracker.Update(graph);

        var metric = tracker.GetMetrics().Entities["faction:merchant"];
        Assert.Equal(0.6, metric.Deviation, precision: 5);
    }

    [Fact]
    public void Update_DeviationCalculation_UnderTarget()
    {
        var targets = CreateTargets(("faction", "merchant", 10));
        var tracker = new PopulationTracker(targets);
        var graph = new GraphStore();
        // Create 3 entities, target is 10 => deviation = (3-10)/10 = -0.7
        for (var i = 0; i < 3; i++)
            graph.CreateEntity(CreateEntity($"f{i}", "faction", "merchant"));

        tracker.Update(graph);

        var metric = tracker.GetMetrics().Entities["faction:merchant"];
        Assert.Equal(-0.7, metric.Deviation, precision: 5);
    }

    [Fact]
    public void Update_TrendCalculation_AfterMultipleUpdates()
    {
        var targets = CreateTargets(("faction", "merchant", 10));
        var tracker = new PopulationTracker(targets);

        // First update: 2 entities
        var graph = new GraphStore();
        graph.CreateEntity(CreateEntity("f0", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("f1", "faction", "merchant"));
        tracker.Update(graph);

        // Second update: 5 entities (add 3 more)
        graph.CreateEntity(CreateEntity("f2", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("f3", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("f4", "faction", "merchant"));
        tracker.Update(graph);

        // Third update: 7 entities (add 2 more)
        graph.CreateEntity(CreateEntity("f5", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("f6", "faction", "merchant"));
        tracker.Update(graph);

        // History: [2, 5, 7]. Deltas: 3, 2. Trend = (3+2)/2 = 2.5
        var metric = tracker.GetMetrics().Entities["faction:merchant"];
        Assert.Equal(2.5, metric.Trend, precision: 5);
    }

    [Fact]
    public void Update_HistoryWindow_StaysAtMax()
    {
        var targets = CreateTargets(("faction", "merchant", 100));
        var tracker = new PopulationTracker(targets, historyWindow: 3);

        var graph = new GraphStore();

        // Update 5 times with increasing entity counts
        for (var tick = 0; tick < 5; tick++)
        {
            graph.CreateEntity(CreateEntity($"f{tick}", "faction", "merchant"));
            tracker.Update(graph);
        }

        // With window=3, history should contain only last 3 entries: [3, 4, 5]
        var metric = tracker.GetMetrics().Entities["faction:merchant"];
        Assert.Equal(3, metric.History.Count);
        Assert.Equal(3, metric.History[0]);
        Assert.Equal(4, metric.History[1]);
        Assert.Equal(5, metric.History[2]);
    }

    [Fact]
    public void GetOutliers_IdentifiesOverpopulated()
    {
        var targets = CreateTargets(("faction", "merchant", 5), ("npc", "warrior", 10));
        var tracker = new PopulationTracker(targets);
        var graph = new GraphStore();

        // 8 merchants (deviation = 0.6 > 0.3 threshold)
        for (var i = 0; i < 8; i++)
            graph.CreateEntity(CreateEntity($"f{i}", "faction", "merchant"));

        // 10 warriors (deviation = 0.0, at target)
        for (var i = 0; i < 10; i++)
            graph.CreateEntity(CreateEntity($"w{i}", "npc", "warrior"));

        tracker.Update(graph);

        var (overpopulated, underpopulated) = tracker.GetOutliers(0.3);
        Assert.Single(overpopulated);
        Assert.Equal("faction", overpopulated[0].Kind);
        Assert.Equal("merchant", overpopulated[0].Subtype);
        Assert.Empty(underpopulated);
    }

    [Fact]
    public void GetOutliers_IdentifiesUnderpopulated()
    {
        var targets = CreateTargets(("faction", "merchant", 10));
        var tracker = new PopulationTracker(targets);
        var graph = new GraphStore();

        // 3 merchants (deviation = -0.7 < -0.3 threshold)
        for (var i = 0; i < 3; i++)
            graph.CreateEntity(CreateEntity($"f{i}", "faction", "merchant"));

        tracker.Update(graph);

        var (overpopulated, underpopulated) = tracker.GetOutliers(0.3);
        Assert.Empty(overpopulated);
        Assert.Single(underpopulated);
        Assert.Equal(-0.7, underpopulated[0].Deviation, precision: 5);
    }

    [Fact]
    public void GetOutliers_SkipsZeroTargetSubtypes()
    {
        // Subtype with no target should never appear as outlier
        var targets = new DistributionTargets();
        targets.Entities["faction"] = new Dictionary<string, int> { ["merchant"] = 0 };
        var tracker = new PopulationTracker(targets);
        var graph = new GraphStore();
        for (var i = 0; i < 10; i++)
            graph.CreateEntity(CreateEntity($"f{i}", "faction", "merchant"));

        tracker.Update(graph);

        var (overpopulated, underpopulated) = tracker.GetOutliers(0.3);
        Assert.Empty(overpopulated);
        Assert.Empty(underpopulated);
    }

    [Fact]
    public void GetSummary_ReturnsCorrectTotals()
    {
        var targets = CreateTargets(("faction", "merchant", 5), ("npc", "warrior", 10));
        var tracker = new PopulationTracker(targets);
        var graph = new GraphStore();

        for (var i = 0; i < 3; i++)
            graph.CreateEntity(CreateEntity($"f{i}", "faction", "merchant"));
        for (var i = 0; i < 7; i++)
            graph.CreateEntity(CreateEntity($"w{i}", "npc", "warrior"));

        // Add a relationship
        graph.AddRelationship(CreateRelationship("f0", "w0"));

        tracker.Update(graph);

        var summary = tracker.GetSummary();
        Assert.Equal(10, summary.TotalEntities);
        Assert.Equal(1, summary.TotalRelationships);
    }

    [Fact]
    public void GetSummary_AvgAndMaxDeviation()
    {
        var targets = CreateTargets(("faction", "merchant", 10), ("npc", "warrior", 10));
        var tracker = new PopulationTracker(targets);
        var graph = new GraphStore();

        // 3 merchants (deviation = -0.7), 10 warriors (deviation = 0.0)
        for (var i = 0; i < 3; i++)
            graph.CreateEntity(CreateEntity($"f{i}", "faction", "merchant"));
        for (var i = 0; i < 10; i++)
            graph.CreateEntity(CreateEntity($"w{i}", "npc", "warrior"));

        tracker.Update(graph);

        var summary = tracker.GetSummary();
        // Avg: (|−0.7| + |0.0|) / 2 = 0.35
        Assert.Equal(0.35, summary.AvgEntityDeviation, precision: 5);
        // Max: 0.7
        Assert.Equal(0.7, summary.MaxEntityDeviation, precision: 5);
    }

    [Fact]
    public void Update_DiscoversNewSubtypesAtRuntime()
    {
        var targets = CreateTargets(("faction", "merchant", 5));
        var tracker = new PopulationTracker(targets);
        var graph = new GraphStore();

        // Create entities of a kind NOT in the distribution targets
        graph.CreateEntity(CreateEntity("r1", "region", "forest"));
        graph.CreateEntity(CreateEntity("r2", "region", "forest"));
        tracker.Update(graph);

        var metrics = tracker.GetMetrics();
        Assert.True(metrics.Entities.ContainsKey("region:forest"));
        Assert.Equal(2, metrics.Entities["region:forest"].Count);
    }

    [Fact]
    public void Update_TracksPressures()
    {
        var targets = CreateTargets(("faction", "merchant", 5));
        var tracker = new PopulationTracker(targets);
        var graph = new GraphStore();
        graph.Pressures["conflict"] = 60;

        tracker.Update(graph);

        var metrics = tracker.GetMetrics();
        Assert.True(metrics.Pressures.ContainsKey("conflict"));
        Assert.Equal(60, metrics.Pressures["conflict"].Value);
        // Target for conflict = 40, deviation = (60-40)/40 = 0.5
        Assert.Equal(0.5, metrics.Pressures["conflict"].Deviation, precision: 5);
    }

    [Fact]
    public void Update_TracksRelationships()
    {
        var targets = CreateTargets(("faction", "merchant", 5));
        var tracker = new PopulationTracker(targets);
        var graph = new GraphStore();
        graph.CreateEntity(CreateEntity("e1", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("e2", "faction", "merchant"));
        graph.AddRelationship(CreateRelationship("e1", "e2", "allied_with"));
        graph.AddRelationship(CreateRelationship("e2", "e1", "rival_of"));

        tracker.Update(graph);

        var metrics = tracker.GetMetrics();
        Assert.Equal(1, metrics.Relationships["allied_with"].Count);
        Assert.Equal(1, metrics.Relationships["rival_of"].Count);
    }

    // =========================================================================
    // DistributionCalculations Tests
    // =========================================================================

    [Fact]
    public void CalculateEntityKindCounts_GroupsByKind()
    {
        var entities = new List<Entity>
        {
            CreateEntity("f1", "faction", "merchant"),
            CreateEntity("f2", "faction", "criminal"),
            CreateEntity("n1", "npc", "warrior"),
        };

        var counts = DistributionCalculations.CalculateEntityKindCounts(entities);

        Assert.Equal(2, counts["faction"]);
        Assert.Equal(1, counts["npc"]);
    }

    [Fact]
    public void CalculateSubtypeDistribution_CountsAndRatios()
    {
        var entities = new List<Entity>
        {
            CreateEntity("f1", "faction", "merchant"),
            CreateEntity("f2", "faction", "merchant"),
            CreateEntity("f3", "faction", "criminal"),
            CreateEntity("n1", "npc", "warrior"),
        };

        var (counts, ratios) = DistributionCalculations.CalculateSubtypeDistribution(entities);

        Assert.Equal(2, counts["faction:merchant"]);
        Assert.Equal(1, counts["faction:criminal"]);
        Assert.Equal(1, counts["npc:warrior"]);
        Assert.Equal(0.5, ratios["faction:merchant"], precision: 5);
        Assert.Equal(0.25, ratios["faction:criminal"], precision: 5);
    }

    [Fact]
    public void CalculateProminenceDistribution_CategorizesByLabel()
    {
        var e1 = CreateEntity("e1", "npc", "warrior", prominence: 0.5); // Forgotten
        var e2 = CreateEntity("e2", "npc", "warrior", prominence: 1.5); // Marginal
        var e3 = CreateEntity("e3", "npc", "warrior", prominence: 2.5); // Recognized
        var e4 = CreateEntity("e4", "npc", "warrior", prominence: 4.5); // Mythic
        var entities = new List<Entity> { e1, e2, e3, e4 };

        var (counts, ratios) = DistributionCalculations.CalculateProminenceDistribution(entities);

        Assert.Equal(1, counts["Forgotten"]);
        Assert.Equal(1, counts["Marginal"]);
        Assert.Equal(1, counts["Recognized"]);
        Assert.Equal(0, counts["Renowned"]);
        Assert.Equal(1, counts["Mythic"]);
        Assert.Equal(0.25, ratios["Forgotten"], precision: 5);
    }

    [Fact]
    public void CalculateRelationshipDistribution_CountsAndDiversity()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateEntity("e1", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("e2", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("e3", "faction", "merchant"));
        graph.AddRelationship(CreateRelationship("e1", "e2", "allied_with"));
        graph.AddRelationship(CreateRelationship("e2", "e3", "allied_with"));
        graph.AddRelationship(CreateRelationship("e1", "e3", "rival_of"));

        var (counts, ratios, diversity) = DistributionCalculations.CalculateRelationshipDistribution(graph);

        Assert.Equal(2, counts["allied_with"]);
        Assert.Equal(1, counts["rival_of"]);
        Assert.True(diversity > 0); // Should have positive entropy for mixed distribution
    }

    [Fact]
    public void CalculateRelationshipDistribution_SingleKind_ZeroDiversity()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateEntity("e1", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("e2", "faction", "merchant"));
        graph.AddRelationship(CreateRelationship("e1", "e2", "allied_with"));

        var (_, _, diversity) = DistributionCalculations.CalculateRelationshipDistribution(graph);

        // Single kind has ratio 1.0, log2(1.0) = 0, so diversity = 0
        Assert.Equal(0, diversity, precision: 5);
    }

    [Fact]
    public void CalculateConnectivityMetrics_IdentifiesIsolatedNodes()
    {
        var graph = new GraphStore();
        graph.CreateEntity(CreateEntity("e1", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("e2", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("e3", "faction", "merchant")); // isolated
        graph.AddRelationship(CreateRelationship("e1", "e2", "allied_with"));

        var (isolated, avg, max, min) = DistributionCalculations.CalculateConnectivityMetrics(graph);

        Assert.Equal(1, isolated); // e3 is isolated
        Assert.Equal(1.0, avg, precision: 5); // e1=1, e2=1 => avg=1
        Assert.Equal(1, max);
        Assert.Equal(1, min);
    }

    [Fact]
    public void CalculateConnectivityMetrics_EmptyGraph_AllZeros()
    {
        var graph = new GraphStore();

        var (isolated, avg, max, min) = DistributionCalculations.CalculateConnectivityMetrics(graph);

        Assert.Equal(0, isolated);
        Assert.Equal(0, avg);
        Assert.Equal(0, max);
        Assert.Equal(0, min);
    }
}
