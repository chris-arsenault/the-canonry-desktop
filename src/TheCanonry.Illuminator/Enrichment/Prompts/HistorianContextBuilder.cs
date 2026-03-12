using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Supporting context types for historian prompt building.
/// </summary>

public sealed record EntitySnapshot(
    string Id,
    string Name,
    string Kind,
    string Subtype,
    string Prominence,
    string Culture,
    string? Description);

public sealed record RelationshipSnapshot(
    string SourceId,
    string SourceName,
    string TargetId,
    string TargetName,
    string Kind);

public sealed record HistorianEditionContext(
    EntitySnapshot Entity,
    string Tone,
    int WordBudget,
    string EntitySummary,
    string RelationshipSummary,
    string ChronicleSourcesSummary,
    string? WorldContext,
    string PreviousAnnotationsSummary);

public sealed record HistorianReviewContext(
    string SourceText,
    string Tone,
    string NoteType,
    string RelationshipSummary,
    string CanonFactsSummary,
    string? WorldDynamics,
    string PreviousNotesSummary);

public sealed record CorpusVoiceDigest(
    IReadOnlyList<string> OverusedOpenings,
    IReadOnlyList<string> OverusedSuperlatives,
    int AverageLength,
    int MinLength,
    int MaxLength);

/// <summary>
/// Assembles context objects for historian tasks.
/// </summary>
public static class HistorianContextBuilder
{
    private static readonly IReadOnlyDictionary<string, int> ProminenceBaseWords =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["forgotten"] = 80,
            ["marginal"] = 100,
            ["recognized"] = 150,
            ["renowned"] = 200,
            ["mythic"] = 300,
        };

    /// <summary>
    /// Compute word budget: base (by prominence) × revision dampening.
    /// Each revision reduces budget by 15% (compounding), minimum 60% of base.
    /// </summary>
    public static int ComputeWordBudget(string prominence, int revisionCount, int baseWords = 150)
    {
        var resolvedBase = ProminenceBaseWords.TryGetValue(prominence, out var found) ? found : baseWords;
        var dampening = Math.Max(0.60, Math.Pow(0.85, revisionCount));
        return (int)Math.Ceiling(resolvedBase * dampening);
    }

    /// <summary>
    /// Build context for historian edition task (entity description revision).
    /// </summary>
    public static HistorianEditionContext BuildEditionContext(
        EntitySnapshot entity,
        HistorianConfig config,
        IReadOnlyList<string> descriptionHistory,
        IReadOnlyList<RelationshipSnapshot> relationships,
        IReadOnlyList<EntitySnapshot> neighbors,
        IReadOnlyList<string> chronicleSources,
        string? worldContext,
        IReadOnlyList<string> previousAnnotations)
    {
        var wordBudget = ComputeWordBudget(entity.Prominence, descriptionHistory.Count);

        var relLines = relationships.Select(r =>
            $"  - {r.Kind} → {r.TargetName} ({r.TargetName})");
        var relationshipSummary = string.Join("\n", relLines);

        var neighborLines = neighbors.Select(n =>
        {
            var snippet = n.Description is { Length: > 200 } d ? d[..200] : (n.Description ?? "");
            return $"  [{n.Kind}] {n.Name}: {snippet}";
        });
        var entitySummary = string.Join("\n", neighborLines);

        var chronicleLines = chronicleSources.Select(s => $"  - {s}");
        var chronicleSourcesSummary = string.Join("\n", chronicleLines);

        var annotationLines = previousAnnotations.Select(a => $"  {a}");
        var previousAnnotationsSummary = string.Join("\n", annotationLines);

        return new HistorianEditionContext(
            Entity: entity,
            Tone: "scholarly",
            WordBudget: wordBudget,
            EntitySummary: entitySummary,
            RelationshipSummary: relationshipSummary,
            ChronicleSourcesSummary: chronicleSourcesSummary,
            WorldContext: worldContext,
            PreviousAnnotationsSummary: previousAnnotationsSummary);
    }

    /// <summary>
    /// Build context for historian review task (chronicle/entity annotation).
    /// </summary>
    public static HistorianReviewContext BuildReviewContext(
        string sourceText,
        EntitySnapshot? entity,
        HistorianConfig config,
        IReadOnlyList<RelationshipSnapshot> relationships,
        IReadOnlyList<EntitySnapshot> neighbors,
        IReadOnlyList<string> canonFacts,
        string? worldDynamics,
        IReadOnlyList<string> previousNotes)
    {
        var relLines = relationships.Select(r =>
            $"  - {r.Kind} → {r.TargetName} ({r.TargetName})");
        var relationshipSummary = string.Join("\n", relLines);

        var factLines = canonFacts.Select(f => $"- {f}");
        var canonFactsSummary = string.Join("\n", factLines);

        var noteLines = previousNotes.Select(n => $"  {n}");
        var previousNotesSummary = string.Join("\n", noteLines);

        return new HistorianReviewContext(
            SourceText: sourceText,
            Tone: "weary",
            NoteType: "entity",
            RelationshipSummary: relationshipSummary,
            CanonFactsSummary: canonFactsSummary,
            WorldDynamics: worldDynamics,
            PreviousNotesSummary: previousNotesSummary);
    }

    /// <summary>
    /// Build corpus voice digest — tracks superlatives, overused openings, length distribution.
    /// </summary>
    public static CorpusVoiceDigest BuildCorpusVoiceDigest(IReadOnlyList<string> existingAnnotations)
    {
        if (existingAnnotations.Count == 0)
        {
            return new CorpusVoiceDigest(
                OverusedOpenings: [],
                OverusedSuperlatives: [],
                AverageLength: 0,
                MinLength: 0,
                MaxLength: 0);
        }

        // Find overused openings (4-word prefix appearing 3+ times)
        var openingCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var annotation in existingAnnotations)
        {
            var words = annotation.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 4) continue;
            var opening = string.Join(" ", words[..4]).TrimEnd('.', ',', ';', ':', '!', '?').ToLowerInvariant();
            openingCounts[opening] = openingCounts.TryGetValue(opening, out var c) ? c + 1 : 1;
        }

        var overusedOpenings = openingCounts
            .Where(kvp => kvp.Value >= 3)
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .Select(kvp => $"\"{kvp.Key}...\" (x{kvp.Value})")
            .ToList();

        // Find overused superlatives
        var superlativePattern = new System.Text.RegularExpressions.Regex(
            @"\bthe (most|only|finest|best|worst|first|last|greatest|single) [^.,;:!?()\[\]""\u2014\n]+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var superlativeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var annotation in existingAnnotations)
        {
            var matches = superlativePattern.Matches(annotation);
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                var norm = m.Value.Trim().ToLowerInvariant();
                superlativeCounts[norm] = superlativeCounts.TryGetValue(norm, out var c) ? c + 1 : 1;
            }
        }

        var overusedSuperlatives = superlativeCounts
            .Where(kvp => kvp.Value >= 2)
            .OrderByDescending(kvp => kvp.Value)
            .Take(6)
            .Select(kvp => $"\"{kvp.Key}\"")
            .ToList();

        // Word count stats
        var wordCounts = existingAnnotations
            .Select(a => a.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
            .ToList();

        var avgLength = (int)Math.Round(wordCounts.Average());
        var minLength = wordCounts.Min();
        var maxLength = wordCounts.Max();

        return new CorpusVoiceDigest(
            OverusedOpenings: overusedOpenings,
            OverusedSuperlatives: overusedSuperlatives,
            AverageLength: avgLength,
            MinLength: minLength,
            MaxLength: maxLength);
    }
}
