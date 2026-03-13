using TheCanonry.Engine.Graph;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Statistics;

/// <summary>
/// Pure functions for calculating distribution statistics from graph data.
/// Extracted for reusability and testability.
/// </summary>
public static class DistributionCalculations
{
    /// <summary>
    /// Calculate entity count by kind.
    /// </summary>
    public static Dictionary<string, int> CalculateEntityKindCounts(IReadOnlyList<Entity> entities)
    {
        var counts = new Dictionary<string, int>();
        foreach (var entity in entities)
        {
            var kind = entity.Kind.ToString();
            counts[kind] = counts.GetValueOrDefault(kind) + 1;
        }
        return counts;
    }

    /// <summary>
    /// Calculate ratios from counts.
    /// </summary>
    public static Dictionary<string, double> CalculateRatios(
        Dictionary<string, int> counts, int total)
    {
        var ratios = new Dictionary<string, double>();
        foreach (var (key, count) in counts)
            ratios[key] = total > 0 ? (double)count / total : 0;
        return ratios;
    }

    /// <summary>
    /// Calculate subtype distribution (counts and ratios by kind:subtype key).
    /// </summary>
    public static (Dictionary<string, int> Counts, Dictionary<string, double> Ratios)
        CalculateSubtypeDistribution(IReadOnlyList<Entity> entities)
    {
        var counts = new Dictionary<string, int>();
        foreach (var entity in entities)
        {
            var key = $"{entity.Kind}:{entity.Subtype}";
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        var ratios = CalculateRatios(counts, entities.Count);
        return (counts, ratios);
    }

    /// <summary>
    /// Calculate prominence distribution (counts and ratios by prominence label).
    /// </summary>
    public static (Dictionary<string, int> Counts, Dictionary<string, double> Ratios)
        CalculateProminenceDistribution(IReadOnlyList<Entity> entities)
    {
        var counts = new Dictionary<string, int>
        {
            ["Forgotten"] = 0,
            ["Marginal"] = 0,
            ["Recognized"] = 0,
            ["Renowned"] = 0,
            ["Mythic"] = 0
        };

        foreach (var entity in entities)
        {
            var label = entity.Prominence.Label;
            counts[label] = counts.GetValueOrDefault(label) + 1;
        }

        var ratios = CalculateRatios(counts, entities.Count);
        return (counts, ratios);
    }

    /// <summary>
    /// Calculate relationship type distribution with Shannon diversity index.
    /// </summary>
    public static (Dictionary<string, int> Counts, Dictionary<string, double> Ratios, double Diversity)
        CalculateRelationshipDistribution(IGraph graph)
    {
        var counts = new Dictionary<string, int>();
        var relationships = graph.GetRelationships();

        foreach (var rel in relationships)
        {
            var kind = rel.Kind.ToString();
            counts[kind] = counts.GetValueOrDefault(kind) + 1;
        }

        var ratios = CalculateRatios(counts, relationships.Count);

        // Shannon entropy for diversity
        var diversity = 0.0;
        foreach (var ratio in ratios.Values)
        {
            if (ratio > 0)
                diversity -= ratio * Math.Log2(ratio);
        }

        return (counts, ratios, diversity);
    }

    /// <summary>
    /// Calculate graph connectivity metrics: isolated nodes, avg/max/min connections.
    /// </summary>
    public static (int IsolatedNodes, double AvgConnections, int MaxConnections, int MinConnections)
        CalculateConnectivityMetrics(IGraph graph)
    {
        var connectionCounts = new Dictionary<string, int>();

        foreach (var rel in graph.GetRelationships())
        {
            var src = rel.SourceId.ToString();
            var dst = rel.TargetId.ToString();
            connectionCounts[src] = connectionCounts.GetValueOrDefault(src) + 1;
            connectionCounts[dst] = connectionCounts.GetValueOrDefault(dst) + 1;
        }

        var entities = graph.GetEntities();
        var isolatedNodes = 0;
        foreach (var entity in entities)
        {
            if (!connectionCounts.ContainsKey(entity.Id.ToString()))
                isolatedNodes++;
        }

        if (connectionCounts.Count == 0)
            return (entities.Count, 0.0, 0, 0);

        var values = connectionCounts.Values;
        var sum = 0;
        var max = int.MinValue;
        var min = int.MaxValue;
        foreach (var v in values)
        {
            sum += v;
            if (v > max) max = v;
            if (v < min) min = v;
        }

        var avg = (double)sum / values.Count;
        return (isolatedNodes, avg, max, min);
    }
}
