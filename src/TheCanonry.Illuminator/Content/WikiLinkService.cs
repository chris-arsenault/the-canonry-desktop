/**
 * WikiLinkService — Entity mention detection and wiki link application
 *
 * Ported from apps/illuminator/webui/src/lib/wikiLinkService.ts
 *
 * Two main operations:
 *   FindEntityMentions — case-insensitive search for entity names, one match per entity
 *   ApplyWikiLinks — wraps matched names in [[...]] brackets
 *   ExtractEntityLinks — regex extraction of already-linked [[...]] spans
 *
 * Normalization: lowercase, collapse non-alphanumeric to hyphens, strip possessive 's,
 * trim leading/trailing hyphens. Matches must be at word boundaries (separated by
 * hyphens in normalized form).
 */

using System.Text.RegularExpressions;

namespace TheCanonry.Illuminator.Content;

public sealed record WikiLinkEntity(string Id, string Name);

public sealed record EntityMention(string EntityId, string EntityName, int Position, int Length);

public sealed record WikiLinkResult(string Content, IReadOnlyList<string> LinkedEntityIds);

/// <summary>
/// Provides entity mention detection and wiki link insertion for markdown content.
/// </summary>
public static class WikiLinkService
{
    private static readonly Regex WikiLinkPattern = new(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Extract entity names already wrapped in [[...]] brackets from content.
    /// Returns the inner text of each match.
    /// </summary>
    public static IReadOnlyList<string> ExtractEntityLinks(string content)
    {
        if (string.IsNullOrEmpty(content))
            return Array.Empty<string>();

        var matches = WikiLinkPattern.Matches(content);
        var results = new List<string>(matches.Count);
        foreach (Match m in matches)
            results.Add(m.Groups[1].Value);
        return results;
    }

    /// <summary>
    /// Find entity name mentions in content using case-insensitive search.
    /// Returns one mention per entity (first occurrence), no overlap filtering.
    /// </summary>
    public static IReadOnlyList<EntityMention> FindEntityMentions(
        string content,
        IReadOnlyList<WikiLinkEntity> entities)
    {
        if (string.IsNullOrEmpty(content) || entities.Count == 0)
            return Array.Empty<EntityMention>();

        var normalizedContent = NormalizeForMatch(content, out var indexMap);
        var results = new List<EntityMention>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entity in entities)
        {
            if (string.IsNullOrEmpty(entity.Name)) continue;
            if (seenIds.Contains(entity.Id)) continue;

            var normalizedName = NormalizeSlug(entity.Name);
            if (string.IsNullOrEmpty(normalizedName)) continue;

            var match = FindFirstBoundaryMatch(normalizedContent, normalizedName);
            if (match < 0) continue;

            var rawStart = indexMap[match];
            var rawEnd = indexMap[match + normalizedName.Length - 1] + 1;

            results.Add(new EntityMention(entity.Id, entity.Name, rawStart, rawEnd - rawStart));
            seenIds.Add(entity.Id);
        }

        return results;
    }

    /// <summary>
    /// Wrap entity mentions in [[...]] brackets.
    /// Returns modified content and list of linked entity IDs (deduped, order of first mention).
    /// Overlapping matches are resolved by taking earlier positions first.
    /// </summary>
    public static WikiLinkResult ApplyWikiLinks(
        string content,
        IReadOnlyList<WikiLinkEntity> entities)
    {
        if (string.IsNullOrEmpty(content) || entities.Count == 0)
            return new WikiLinkResult(content, Array.Empty<string>());

        var normalizedContent = NormalizeForMatch(content, out var indexMap);
        var mentions = new List<(string EntityId, int RawStart, int RawEnd)>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entity in entities)
        {
            if (string.IsNullOrEmpty(entity.Name)) continue;
            if (seenIds.Contains(entity.Id)) continue;

            var normalizedName = NormalizeSlug(entity.Name);
            if (string.IsNullOrEmpty(normalizedName)) continue;

            var match = FindFirstBoundaryMatch(normalizedContent, normalizedName);
            if (match < 0) continue;

            var rawStart = indexMap[match];
            var rawEnd = indexMap[match + normalizedName.Length - 1] + 1;
            mentions.Add((entity.Id, rawStart, rawEnd));
            seenIds.Add(entity.Id);
        }

        if (mentions.Count == 0)
            return new WikiLinkResult(content, Array.Empty<string>());

        // Sort by position, filter overlaps (keep earlier)
        mentions.Sort((a, b) => a.RawStart.CompareTo(b.RawStart));
        var nonOverlapping = FilterOverlaps(mentions);

        // Build result string by inserting [[ ]] around matches (forward order)
        var sb = new System.Text.StringBuilder(content.Length + nonOverlapping.Count * 4);
        var cursor = 0;
        var linkedIds = new List<string>();

        foreach (var (entityId, rawStart, rawEnd) in nonOverlapping)
        {
            if (rawStart < cursor) continue; // safety guard
            sb.Append(content, cursor, rawStart - cursor);
            sb.Append("[[");
            sb.Append(content, rawStart, rawEnd - rawStart);
            sb.Append("]]");
            cursor = rawEnd;
            linkedIds.Add(entityId);
        }
        sb.Append(content, cursor, content.Length - cursor);

        return new WikiLinkResult(sb.ToString(), linkedIds);
    }

    // -------------------------------------------------------------------------
    // Normalization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Normalize text for matching: lowercase, collapse non-alphanumeric to hyphens,
    /// strip possessive 's, trim leading/trailing hyphens.
    /// Also builds an indexMap: normalized[i] originated from original[indexMap[i]].
    /// </summary>
    private static string NormalizeForMatch(string text, out int[] indexMap)
    {
        var chars = new List<char>(text.Length);
        var map = new List<int>(text.Length);
        var prevSep = true;

        for (var i = 0; i < text.Length; i++)
        {
            var lower = char.ToLowerInvariant(text[i]);
            if (IsAlphaNumeric(lower))
            {
                chars.Add(lower);
                map.Add(i);
                prevSep = false;
            }
            else if (IsPossessiveSuffix(text, i))
            {
                // Skip apostrophe + 's' — no separator
                i += 1;
            }
            else if (!prevSep)
            {
                chars.Add('-');
                map.Add(i);
                prevSep = true;
            }
        }

        // Trim leading hyphens
        while (chars.Count > 0 && chars[0] == '-')
        {
            chars.RemoveAt(0);
            map.RemoveAt(0);
        }
        // Trim trailing hyphens
        while (chars.Count > 0 && chars[^1] == '-')
        {
            chars.RemoveAt(chars.Count - 1);
            map.RemoveAt(map.Count - 1);
        }

        indexMap = map.ToArray();
        return new string(chars.ToArray());
    }

    private static string NormalizeSlug(string text) => NormalizeForMatch(text, out _);

    private static bool IsAlphaNumeric(char c) =>
        (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');

    private static bool IsApostrophe(char c) =>
        c == '\'' || c == '\u2019' || c == '\u2018';

    private static bool IsPossessiveSuffix(string text, int i)
    {
        if (!IsApostrophe(text[i])) return false;
        if (i + 1 >= text.Length) return false;
        if (char.ToLowerInvariant(text[i + 1]) != 's') return false;
        if (i + 2 >= text.Length) return true;
        return !IsAlphaNumeric(char.ToLowerInvariant(text[i + 2]));
    }

    // -------------------------------------------------------------------------
    // Matching helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Find first occurrence of normalizedPattern in normalizedContent at a word boundary.
    /// Returns the start index in normalizedContent, or -1 if not found.
    /// </summary>
    private static int FindFirstBoundaryMatch(string normalizedContent, string normalizedPattern)
    {
        var start = 0;
        while (true)
        {
            var idx = normalizedContent.IndexOf(normalizedPattern, start, StringComparison.Ordinal);
            if (idx < 0) return -1;

            if (IsBoundaryMatch(normalizedContent, idx, idx + normalizedPattern.Length))
                return idx;

            start = idx + 1;
        }
    }

    private static bool IsBoundaryMatch(string normalized, int start, int end)
    {
        var beforeOk = start == 0 || normalized[start - 1] == '-';
        var afterOk = end == normalized.Length || normalized[end] == '-';
        return beforeOk && afterOk;
    }

    private static IReadOnlyList<(string EntityId, int RawStart, int RawEnd)> FilterOverlaps(
        IReadOnlyList<(string EntityId, int RawStart, int RawEnd)> sorted)
    {
        var result = new List<(string, int, int)>();
        var lastEnd = -1;
        foreach (var m in sorted)
        {
            if (m.RawStart < lastEnd) continue;
            result.Add(m);
            lastEnd = m.RawEnd;
        }
        return result;
    }
}
