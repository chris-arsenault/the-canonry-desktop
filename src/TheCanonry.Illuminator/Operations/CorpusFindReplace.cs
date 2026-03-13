/**
 * CorpusFindReplace — literal search across corpus contexts with selective replacement.
 *
 * Searches chronicles, entities, and static pages for a query string.
 * Returns structured match records with ±80-char context.
 * Replacement is applied at specified positions in reverse order.
 */

using TheCanonry.Persistence.Entities;
using ChronicleEntity = TheCanonry.Persistence.Entities.Chronicle;

namespace TheCanonry.Illuminator.Operations;

public static class CorpusFindReplace
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    public const int ContextRadius = 80;

    // -------------------------------------------------------------------------
    // Types
    // -------------------------------------------------------------------------

    public enum SearchContext
    {
        ChronicleContent,
        ChronicleTitles,
        EntityDescriptions,
        StaticPageContent,
        EraNarrativeContent,
    }

    public sealed record CorpusMatch(
        string Id,
        SearchContext Context,
        string SourceId,
        string SourceName,
        string Field,
        int Position,
        string ContextBefore,
        string MatchedText,
        string ContextAfter);

    public sealed record FindReplaceResult(
        int TotalMatches,
        int Applied,
        int Skipped);

    // -------------------------------------------------------------------------
    // Search
    // -------------------------------------------------------------------------

    /// <summary>
    /// Searches across the specified contexts for all literal occurrences of
    /// <paramref name="query"/>. Returns one <see cref="CorpusMatch"/> per occurrence
    /// with a unique <c>Id</c> and ±<see cref="ContextRadius"/> character context.
    /// </summary>
    public static IReadOnlyList<CorpusMatch> Search(
        string query,
        bool caseSensitive,
        IReadOnlyList<SearchContext> contexts,
        IReadOnlyList<PersistedEntity>? entities = null,
        IReadOnlyList<ChronicleEntity>? chronicles = null,
        IReadOnlyList<StaticPageEntity>? staticPages = null)
    {
        if (string.IsNullOrEmpty(query)) return [];

        var comparison = caseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        var results = new List<CorpusMatch>();
        var matchSeq = 0;

        foreach (var ctx in contexts)
        {
            switch (ctx)
            {
                case SearchContext.ChronicleContent when chronicles is not null:
                    foreach (var c in chronicles)
                        ScanField(c.Content, c.Id.ToString(), c.Title ?? $"Chronicle {c.Id}",
                            ctx, "Content", query, comparison, results, ref matchSeq);
                    break;

                case SearchContext.ChronicleTitles when chronicles is not null:
                    foreach (var c in chronicles)
                        ScanField(c.Title ?? "", c.Id.ToString(), c.Title ?? $"Chronicle {c.Id}",
                            ctx, "Title", query, comparison, results, ref matchSeq);
                    break;

                case SearchContext.EntityDescriptions when entities is not null:
                    foreach (var e in entities)
                        ScanField(e.Description, e.Id, e.Name,
                            ctx, "Description", query, comparison, results, ref matchSeq);
                    break;

                case SearchContext.StaticPageContent when staticPages is not null:
                    foreach (var p in staticPages)
                        ScanField(p.Content, p.PageId, p.Title,
                            ctx, "Content", query, comparison, results, ref matchSeq);
                    break;

                // EraNarrativeContent — not wired to a repository parameter here;
                // callers that have era narrative data should pre-scan and merge results.
            }
        }

        return results;
    }

    // -------------------------------------------------------------------------
    // Replacement
    // -------------------------------------------------------------------------

    /// <summary>
    /// Replaces the query at each of the given <paramref name="positions"/> within
    /// <paramref name="text"/>. Applied in descending position order so that earlier
    /// indices remain valid after each substitution.
    /// </summary>
    public static string ApplyReplacements(
        string text,
        string replacement,
        IReadOnlyList<int> positions,
        int matchLength)
    {
        if (positions.Count == 0 || matchLength <= 0) return text;

        var sorted = positions
            .OrderByDescending(p => p)
            .ToList();

        foreach (var pos in sorted)
        {
            if (pos < 0 || pos + matchLength > text.Length) continue;
            text = text[..pos] + replacement + text[(pos + matchLength)..];
        }

        return text;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static void ScanField(
        string? text,
        string sourceId,
        string sourceName,
        SearchContext ctx,
        string field,
        string query,
        StringComparison comparison,
        List<CorpusMatch> results,
        ref int matchSeq)
    {
        if (string.IsNullOrEmpty(text)) return;

        var start = 0;
        while (true)
        {
            var idx = text.IndexOf(query, start, comparison);
            if (idx < 0) break;

            var beforeStart = Math.Max(0, idx - ContextRadius);
            var afterEnd    = Math.Min(text.Length, idx + query.Length + ContextRadius);

            var before  = text[beforeStart..idx];
            var matched = text.Substring(idx, query.Length);
            var after   = text[(idx + query.Length)..afterEnd];

            results.Add(new CorpusMatch(
                Id:            $"match-{++matchSeq:D6}",
                Context:       ctx,
                SourceId:      sourceId,
                SourceName:    sourceName,
                Field:         field,
                Position:      idx,
                ContextBefore: before,
                MatchedText:   matched,
                ContextAfter:  after));

            start = idx + query.Length;
        }
    }
}
