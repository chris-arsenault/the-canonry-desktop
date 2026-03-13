using TheCanonry.Engine.Graph;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Selection;

/// <summary>
/// Framework-level weighted target selection that prevents super-hub formation.
/// Score-based selection with exponential penalties for existing connections.
/// </summary>
public static class TargetSelector
{
    /// <summary>
    /// Select entities of a given kind using weighted scoring with hub penalties.
    /// </summary>
    /// <param name="graph">World graph to select from.</param>
    /// <param name="kind">Entity kind to select.</param>
    /// <param name="count">Number of entities to select.</param>
    /// <param name="bias">Selection preferences and penalties (null uses defaults).</param>
    public static SelectionResult SelectTargets(
        IGraph graph, EntityKind kind, int count, SelectionBias? bias = null)
    {
        bias ??= new SelectionBias();

        // Find all candidate entities of the requested kind
        var candidates = graph.GetEntitiesByKind(kind);

        if (candidates.Count == 0)
            return EmptyResult();

        // Score all candidates
        var scored = new List<(Entity Entity, double Score)>();
        foreach (var entity in candidates)
            scored.Add((entity, ScoreCandidate(graph, entity, bias)));

        // Apply hard filters
        var filtered = ApplyHardFilters(graph, scored, bias);

        // Sort by score descending
        filtered.Sort((a, b) => b.Score.CompareTo(a.Score));

        if (filtered.Count == 0)
            return EmptyResult();

        // Select top N candidates
        var selected = new List<Entity>();
        for (var i = 0; i < Math.Min(count, filtered.Count); i++)
            selected.Add(filtered[i].Entity);

        // Calculate diagnostics
        var scores = filtered.Select(s => s.Score).ToList();
        return new SelectionResult
        {
            Existing = selected,
            Diagnostics = new SelectionDiagnostics
            {
                CandidatesEvaluated = candidates.Count,
                BestScore = scores.Max(),
                WorstScore = scores.Min(),
                AvgScore = scores.Average()
            }
        };
    }

    /// <summary>
    /// Score a candidate entity. Higher score = more desirable target.
    /// </summary>
    private static double ScoreCandidate(IGraph graph, Entity entity, SelectionBias bias)
    {
        var score = 1.0;
        score = ApplyPreferenceBias(entity, bias.Prefer, score);
        score = ApplyAvoidancePenalties(graph, entity, bias.Avoid, score);
        return Math.Max(0, score);
    }

    private static double ApplyPreferenceBias(
        Entity entity, SelectionPreferences prefer, double score)
    {
        var boost = prefer.PreferenceBoost;

        if (prefer.Subtypes.Contains(entity.Subtype))
            score *= boost;

        if (prefer.Tags.Any(tag => entity.Tags.Contains(tag)))
            score *= boost;

        if (prefer.Prominence.Contains(entity.Prominence.Label))
            score *= boost;

        return score;
    }

    private static double ApplyAvoidancePenalties(
        IGraph graph, Entity entity, SelectionAvoidance avoid, double score)
    {
        // Penalize entities with specific relationship kinds
        var penalizedCount = 0;
        if (avoid.RelationshipKinds.Count > 0)
        {
            var rels = graph.GetEntityRelationships(entity.Id, Direction.Both);
            foreach (var rel in rels)
            {
                if (avoid.RelationshipKinds.Contains(rel.Kind.ToString()))
                    penalizedCount++;
            }
        }

        var strength = avoid.HubPenaltyStrength;
        if (penalizedCount > 0)
            score *= 1.0 / (1.0 + Math.Pow(penalizedCount, strength));

        // General hub penalty: penalize entities with more than 5 total connections
        var totalLinks = graph.GetEntityRelationships(entity.Id, Direction.Both).Count;
        if (totalLinks > 5)
            score *= 1.0 / (1.0 + Math.Pow(totalLinks - 5, 0.5));

        return score;
    }

    /// <summary>
    /// Apply hard filters that completely exclude candidates.
    /// </summary>
    private static List<(Entity Entity, double Score)> ApplyHardFilters(
        IGraph graph,
        List<(Entity Entity, double Score)> scored,
        SelectionBias bias)
    {
        var filtered = new List<(Entity Entity, double Score)>(scored);

        // Culture hard filters
        if (bias.Culture.Require is not null)
            filtered.RemoveAll(s => s.Entity.Culture.ToString() != bias.Culture.Require);

        if (bias.Culture.Exclude.Count > 0)
            filtered.RemoveAll(s => bias.Culture.Exclude.Contains(s.Entity.Culture.ToString()));

        // Max total relationships hard cap
        if (bias.Avoid.MaxTotalRelationships > 0)
        {
            filtered.RemoveAll(s =>
                graph.GetEntityRelationships(s.Entity.Id, Direction.Both).Count
                >= bias.Avoid.MaxTotalRelationships);
        }

        return filtered;
    }

    private static SelectionResult EmptyResult() => new()
    {
        Existing = [],
        Diagnostics = new SelectionDiagnostics
        {
            CandidatesEvaluated = 0,
            BestScore = 0,
            WorstScore = 0,
            AvgScore = 0
        }
    };
}
