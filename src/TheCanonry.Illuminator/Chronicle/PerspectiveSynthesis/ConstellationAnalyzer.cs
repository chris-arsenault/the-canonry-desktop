namespace TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;

/// <summary>
/// Analyzes a set of entities and relationships to derive structural signals
/// for perspective synthesis. Pure logic — no I/O, no LLM calls.
/// </summary>
public static class ConstellationAnalyzer
{
    /// <summary>
    /// Analyze the entity constellation and return structural signals.
    /// </summary>
    public static EntityConstellation Analyze(
        IReadOnlyList<EntityContext> entities,
        IReadOnlyList<RelationshipContext> relationships,
        EraContext? focalEra = null)
    {
        var cultures = CountCultures(entities);
        var (topCulture, cultureBalance) = ClassifyCultures(cultures, entities.Count);

        var kinds = CountKinds(entities);
        var (topKind, kindFocus) = ClassifyKinds(kinds);

        var relationshipKinds = CountRelationshipKinds(relationships);

        // EntityContext has no tags field; tag frequency is always empty
        var tagFrequency = new Dictionary<string, int>(0);
        var prominentTags = Array.Empty<string>();

        // EntityContext has no coordinates field; spatial analysis defaults to tight
        (double X, double Y)? coordinateCentroid = null;
        var spatialSpread = SpatialSpread.Tight;

        var eraSpan = focalEra is not null ? EraSpan.Single : EraSpan.Single;

        var focusSummary = BuildFocusSummary(
            cultureBalance,
            topCulture,
            kindFocus,
            prominentTags,
            relationshipKinds);

        return new EntityConstellation
        {
            Cultures = cultures,
            DominantCulture = cultureBalance != CultureBalance.Mixed ? topCulture : null,
            CultureBalance = cultureBalance,
            Kinds = kinds,
            DominantKind = topKind,
            KindFocus = kindFocus,
            TagFrequency = tagFrequency,
            ProminentTags = prominentTags,
            RelationshipKinds = relationshipKinds,
            FocalEraId = focalEra?.Id,
            EraSpan = eraSpan,
            CoordinateCentroid = coordinateCentroid,
            SpatialSpread = spatialSpread,
            FocusSummary = focusSummary,
        };
    }

    // =========================================================================
    // Culture analysis
    // =========================================================================

    private static IReadOnlyDictionary<string, int> CountCultures(IReadOnlyList<EntityContext> entities)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entities)
        {
            if (e.Culture is { Length: > 0 } culture)
                counts[culture] = counts.TryGetValue(culture, out var n) ? n + 1 : 1;
        }
        return counts;
    }

    private static (string? TopCulture, CultureBalance Balance) ClassifyCultures(
        IReadOnlyDictionary<string, int> cultures,
        int total)
    {
        if (total == 0 || cultures.Count == 0)
            return (null, CultureBalance.Mixed);

        string? topCulture = null;
        var topCount = 0;
        foreach (var (culture, count) in cultures)
        {
            if (count > topCount)
            {
                topCount = count;
                topCulture = culture;
            }
        }

        var ratio = (double)topCount / total;
        var balance = ratio > 0.8 ? CultureBalance.Single
            : ratio > 0.5 ? CultureBalance.Dominant
            : CultureBalance.Mixed;

        return (topCulture, balance);
    }

    // =========================================================================
    // Kind analysis
    // =========================================================================

    private static IReadOnlyDictionary<string, int> CountKinds(IReadOnlyList<EntityContext> entities)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entities)
        {
            if (e.Kind is { Length: > 0 })
                counts[e.Kind] = counts.TryGetValue(e.Kind, out var n) ? n + 1 : 1;
        }
        return counts;
    }

    private static (string? TopKind, KindFocus Focus) ClassifyKinds(IReadOnlyDictionary<string, int> kinds)
    {
        if (kinds.Count == 0)
            return (null, KindFocus.Mixed);

        string? topKind = null;
        var topCount = 0;
        var total = 0;
        foreach (var (kind, count) in kinds)
        {
            total += count;
            if (count > topCount)
            {
                topCount = count;
                topKind = kind;
            }
        }

        if (total == 0)
            return (topKind, KindFocus.Mixed);

        var ratio = (double)topCount / total;
        if (ratio <= 0.5)
            return (topKind, KindFocus.Mixed);

        var focus = topKind?.ToLowerInvariant() switch
        {
            "npc" or "character" => KindFocus.Character,
            "location" or "place" => KindFocus.Place,
            "artifact" or "object" or "item" => KindFocus.Object,
            "occurrence" or "event" => KindFocus.Event,
            _ => KindFocus.Mixed,
        };

        return (topKind, focus);
    }

    // =========================================================================
    // Relationship analysis
    // =========================================================================

    private static IReadOnlyDictionary<string, int> CountRelationshipKinds(
        IReadOnlyList<RelationshipContext> relationships)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in relationships)
        {
            if (r.Kind is { Length: > 0 })
                counts[r.Kind] = counts.TryGetValue(r.Kind, out var n) ? n + 1 : 1;
        }
        return counts;
    }

    // =========================================================================
    // Focus summary
    // =========================================================================

    private static string BuildFocusSummary(
        CultureBalance cultureBalance,
        string? topCulture,
        KindFocus kindFocus,
        IReadOnlyList<string> prominentTags,
        IReadOnlyDictionary<string, int> relationshipKinds)
    {
        var parts = new List<string>(4);

        parts.Add(cultureBalance switch
        {
            CultureBalance.Single when topCulture is not null => $"{topCulture}-focused",
            CultureBalance.Dominant when topCulture is not null => $"{topCulture}-dominant",
            _ => "cross-cultural",
        });

        if (kindFocus != KindFocus.Mixed)
            parts.Add($"{kindFocus.ToString().ToLowerInvariant()}-centered");

        if (relationshipKinds.Count > 0)
        {
            var relKinds = string.Join(", ", relationshipKinds.Keys.Take(3));
            parts.Add($"relationships: {relKinds}");
        }

        if (prominentTags.Count > 0)
        {
            var tags = string.Join(", ", prominentTags.Take(3));
            parts.Add($"themes: {tags}");
        }

        return string.Join(", ", parts);
    }
}
