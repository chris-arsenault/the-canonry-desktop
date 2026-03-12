namespace TheCanonry.Engine.Statistics;

using TheCanonry.Engine.Graph;

/// <summary>
/// Real-time monitoring of entity/relationship populations for feedback control.
/// Tracks counts, targets, deviations, and trends to enable homeostatic regulation.
/// </summary>
public class PopulationTracker
{
    private readonly PopulationMetrics _metrics = new();
    private readonly DistributionTargets _distributionTargets;
    private readonly int _historyWindow;

    public PopulationTracker(DistributionTargets distributionTargets, int historyWindow = 10)
    {
        _distributionTargets = distributionTargets;
        _historyWindow = historyWindow;
        InitializeSubtypeMetrics();
    }

    /// <summary>
    /// Initialize metrics for all known subtypes from distribution targets.
    /// This ensures feedback loops can find metrics even for zero-count subtypes.
    /// </summary>
    private void InitializeSubtypeMetrics()
    {
        foreach (var (kind, subtypes) in _distributionTargets.Entities)
        {
            foreach (var (subtype, target) in subtypes)
            {
                var key = $"{kind}:{subtype}";
                _metrics.Entities[key] = new EntityMetric
                {
                    Kind = kind,
                    Subtype = subtype,
                    Count = 0,
                    Target = target,
                    Deviation = target > 0 ? -1.0 : 0.0,
                    Trend = 0
                };
            }
        }
    }

    /// <summary>
    /// Update metrics from current graph state.
    /// </summary>
    public void Update(IGraph graph)
    {
        _metrics.Tick = graph.Tick;
        UpdateEntityMetrics(graph);
        UpdateRelationshipMetrics(graph);
        UpdatePressureMetrics(graph);
    }

    private void UpdateEntityMetrics(IGraph graph)
    {
        // Count entities by kind:subtype
        var entityCounts = new Dictionary<string, int>();
        foreach (var entity in graph.GetEntities())
        {
            var key = $"{entity.Kind}:{entity.Subtype}";
            entityCounts[key] = entityCounts.GetValueOrDefault(key) + 1;
        }

        // Update ALL existing metrics (including zero-count subtypes from initialization)
        foreach (var (key, existing) in _metrics.Entities)
        {
            var count = entityCounts.GetValueOrDefault(key);
            var target = _distributionTargets.GetTarget(existing.Kind, existing.Subtype);

            // Calculate deviation: (count - target) / max(target, 1)
            var deviation = target > 0 ? (double)(count - target) / target : 0.0;

            // Update history and calculate trend
            existing.History.Add(count);
            if (existing.History.Count > _historyWindow)
                existing.History.RemoveAt(0);

            existing.Count = count;
            existing.Target = target;
            existing.Deviation = deviation;
            existing.Trend = CalculateTrend(existing.History);
        }

        // Add any NEW subtypes discovered at runtime (not in distribution targets)
        foreach (var (key, count) in entityCounts)
        {
            if (_metrics.Entities.ContainsKey(key))
                continue;

            var parts = key.Split(':');
            var kind = parts[0];
            var subtype = parts.Length > 1 ? parts[1] : "";
            var target = _distributionTargets.GetTarget(kind, subtype);
            var deviation = target > 0 ? (double)(count - target) / target : 0.0;

            var metric = new EntityMetric
            {
                Kind = kind,
                Subtype = subtype,
                Count = count,
                Target = target,
                Deviation = deviation,
                Trend = 0
            };
            metric.History.Add(count);
            _metrics.Entities[key] = metric;
        }
    }

    private void UpdateRelationshipMetrics(IGraph graph)
    {
        // Group relationships by kind
        var relationshipCounts = new Dictionary<string, int>();
        foreach (var rel in graph.GetRelationships())
        {
            var kind = rel.Kind.ToString();
            relationshipCounts[kind] = relationshipCounts.GetValueOrDefault(kind) + 1;
        }

        foreach (var (kind, count) in relationshipCounts)
        {
            if (!_metrics.Relationships.TryGetValue(kind, out var existing))
            {
                existing = new RelationshipMetric { Kind = kind };
                _metrics.Relationships[kind] = existing;
            }

            // Relationship targets are not configured; deviation stays 0
            var target = 0;
            var deviation = target > 0 ? (double)(count - target) / target : 0.0;

            existing.History.Add(count);
            if (existing.History.Count > _historyWindow)
                existing.History.RemoveAt(0);

            existing.Count = count;
            existing.Target = target;
            existing.Deviation = deviation;
            existing.Trend = CalculateTrend(existing.History);
        }
    }

    private void UpdatePressureMetrics(IGraph graph)
    {
        foreach (var (id, value) in graph.Pressures)
        {
            if (!_metrics.Pressures.TryGetValue(id, out var existing))
            {
                existing = new PressureMetric { Id = id };
                _metrics.Pressures[id] = existing;
            }

            var target = GetPressureTarget(id);
            var deviation = target > 0 ? (value - target) / target : 0.0;

            existing.History.Add(value);
            if (existing.History.Count > _historyWindow)
                existing.History.RemoveAt(0);

            existing.Value = value;
            existing.Target = target;
            existing.Deviation = deviation;
            existing.Trend = CalculateTrendDouble(existing.History);
        }
    }

    /// <summary>
    /// Calculate trend as moving average of deltas between consecutive history entries.
    /// </summary>
    private static double CalculateTrend(List<int> history)
    {
        if (history.Count < 2) return 0;

        var sumDeltas = 0.0;
        for (var i = 1; i < history.Count; i++)
            sumDeltas += history[i] - history[i - 1];

        return sumDeltas / (history.Count - 1);
    }

    private static double CalculateTrendDouble(List<double> history)
    {
        if (history.Count < 2) return 0;

        var sumDeltas = 0.0;
        for (var i = 1; i < history.Count; i++)
            sumDeltas += history[i] - history[i - 1];

        return sumDeltas / (history.Count - 1);
    }

    private static double GetPressureTarget(string id)
    {
        return id switch
        {
            "resource_scarcity" => 30,
            "conflict" => 40,
            "magical_instability" => 25,
            "cultural_tension" => 35,
            "stability" => 60,
            "external_threat" => 15,
            _ => 50
        };
    }

    /// <summary>
    /// Get current metrics snapshot.
    /// </summary>
    public PopulationMetrics GetMetrics() => _metrics;

    /// <summary>
    /// Get entities that are significantly over or under their target.
    /// </summary>
    /// <param name="threshold">Minimum absolute deviation to qualify as an outlier (default 0.3 = 30%).</param>
    public (IReadOnlyList<EntityMetric> Overpopulated, IReadOnlyList<EntityMetric> Underpopulated)
        GetOutliers(double threshold = 0.3)
    {
        var overpopulated = new List<EntityMetric>();
        var underpopulated = new List<EntityMetric>();

        foreach (var metric in _metrics.Entities.Values)
        {
            if (metric.Target == 0) continue;

            if (metric.Deviation > threshold)
                overpopulated.Add(metric);
            else if (metric.Deviation < -threshold)
                underpopulated.Add(metric);
        }

        return (overpopulated, underpopulated);
    }

    /// <summary>
    /// Get summary statistics across all tracked populations.
    /// </summary>
    public PopulationSummary GetSummary()
    {
        var totalEntities = 0;
        var totalRelationships = 0;
        var sumDeviations = 0.0;
        var maxDeviation = 0.0;
        var entityMetricCount = 0;

        foreach (var metric in _metrics.Entities.Values)
        {
            totalEntities += metric.Count;
            if (metric.Target > 0)
            {
                sumDeviations += Math.Abs(metric.Deviation);
                maxDeviation = Math.Max(maxDeviation, Math.Abs(metric.Deviation));
                entityMetricCount++;
            }
        }

        foreach (var metric in _metrics.Relationships.Values)
            totalRelationships += metric.Count;

        var pressureDeviations = new Dictionary<string, double>();
        foreach (var metric in _metrics.Pressures.Values)
            pressureDeviations[metric.Id] = metric.Deviation;

        return new PopulationSummary
        {
            TotalEntities = totalEntities,
            TotalRelationships = totalRelationships,
            AvgEntityDeviation = entityMetricCount > 0 ? sumDeviations / entityMetricCount : 0,
            MaxEntityDeviation = maxDeviation,
            PressureDeviations = pressureDeviations
        };
    }
}

/// <summary>
/// Summary statistics for the current population state.
/// </summary>
public class PopulationSummary
{
    public int TotalEntities { get; init; }
    public int TotalRelationships { get; init; }
    public double AvgEntityDeviation { get; init; }
    public double MaxEntityDeviation { get; init; }
    public Dictionary<string, double> PressureDeviations { get; init; } = [];
}
