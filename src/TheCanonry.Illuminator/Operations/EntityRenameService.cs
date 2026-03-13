/**
 * EntityRenameService — simplified entity rename with cross-store reference scanning.
 *
 * Scans entities, chronicles, narrative events, and static pages for occurrences
 * of an entity's old name. Builds case-preserving replacement patches. Applies
 * patches in reverse position order to preserve string indices.
 *
 * Simplifications vs. TS source (entityRenameScan.ts, entityRenamePatchBuild.ts):
 *   - No Aho-Corasick — plain case-insensitive IndexOf
 *   - No grammar adjustment (a/an, possessives)
 *   - No partial name fragment scanning — full name only
 *   - No tier system — MatchSource enum only
 *   - No metadata patching (roleAssignments, lens, directives) — text fields only
 */

using TheCanonry.Persistence.Entities;
using ChronicleEntity = TheCanonry.Persistence.Entities.Chronicle;

namespace TheCanonry.Illuminator.Operations;

public static class EntityRenameService
{
    // -------------------------------------------------------------------------
    // Types
    // -------------------------------------------------------------------------

    public enum MatchType { FullName, PartialName, Metadata }

    public enum MatchSource { Entity, Chronicle, NarrativeEvent, StaticPage }

    public sealed record RenameMatch(
        string SourceId,
        string SourceName,
        MatchSource Source,
        string Field,
        MatchType Type,
        string MatchedText,
        int Position,
        string ContextBefore,
        string ContextAfter);

    public sealed record RenameScanResult(
        string EntityId,
        string OldName,
        IReadOnlyList<RenameMatch> Matches);

    public sealed record RenamePatch(
        string SourceId,
        MatchSource Source,
        string Field,
        int Position,
        int OriginalLength,
        string Replacement);

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const int ContextRadius = 60;

    // -------------------------------------------------------------------------
    // Scanning
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scans all stores for references to the entity's old name.
    /// Returns a <see cref="RenameScanResult"/> containing all matches.
    /// </summary>
    public static RenameScanResult ScanReferences(
        string entityId,
        string oldName,
        IReadOnlyList<PersistedEntity> entities,
        IReadOnlyList<ChronicleEntity> chronicles,
        IReadOnlyList<NarrativeEventEntity>? narrativeEvents = null,
        IReadOnlyList<StaticPageEntity>? staticPages = null)
    {
        if (string.IsNullOrEmpty(oldName))
            return new RenameScanResult(entityId, oldName, []);

        var matches = new List<RenameMatch>();

        // --- Entities ---
        foreach (var entity in entities)
        {
            var sourceId = entity.Id;
            var sourceName = entity.Name;
            ScanTextField(entity.Name,        sourceId, sourceName, MatchSource.Entity, "Name",        oldName, matches);
            ScanTextField(entity.Description, sourceId, sourceName, MatchSource.Entity, "Description", oldName, matches);
            ScanTextField(entity.Summary,     sourceId, sourceName, MatchSource.Entity, "Summary",     oldName, matches);
        }

        // --- Chronicles ---
        foreach (var chronicle in chronicles)
        {
            var sourceId   = chronicle.Id.ToString();
            var sourceName = chronicle.Title ?? $"Chronicle {chronicle.Id}";
            ScanTextField(chronicle.Content,             sourceId, sourceName, MatchSource.Chronicle, "Content", oldName, matches);
            ScanTextField(chronicle.Summary ?? "",       sourceId, sourceName, MatchSource.Chronicle, "Summary", oldName, matches);
            ScanTextField(chronicle.Title   ?? "",       sourceId, sourceName, MatchSource.Chronicle, "Title",   oldName, matches);
        }

        // --- Narrative Events ---
        if (narrativeEvents is not null)
        {
            foreach (var ev in narrativeEvents)
            {
                var sourceId   = ev.Id.ToString();
                var sourceName = $"Event {ev.Id}";
                ScanTextField(ev.Description, sourceId, sourceName, MatchSource.NarrativeEvent, "Description", oldName, matches);
                ScanTextField(ev.Action,      sourceId, sourceName, MatchSource.NarrativeEvent, "Action",      oldName, matches);
            }
        }

        // --- Static Pages ---
        if (staticPages is not null)
        {
            foreach (var page in staticPages)
            {
                var sourceId   = page.PageId;
                var sourceName = page.Title;
                ScanTextField(page.Content, sourceId, sourceName, MatchSource.StaticPage, "Content", oldName, matches);
                ScanTextField(page.Title,   sourceId, sourceName, MatchSource.StaticPage, "Title",   oldName, matches);
            }
        }

        return new RenameScanResult(entityId, oldName, matches);
    }

    // -------------------------------------------------------------------------
    // Patch building
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds text replacement patches from the accepted matches in a scan result.
    /// Case-preserving: if the matched text is all-uppercase the replacement is
    /// uppercased; if all-lowercase it is lowercased; otherwise used as-is.
    /// </summary>
    public static IReadOnlyList<RenamePatch> BuildPatches(
        RenameScanResult scanResult,
        string newName)
    {
        var patches = new List<RenamePatch>();

        foreach (var match in scanResult.Matches)
        {
            var replacement = PreserveCase(match.MatchedText, newName);
            patches.Add(new RenamePatch(
                match.SourceId,
                match.Source,
                match.Field,
                match.Position,
                match.MatchedText.Length,
                replacement));
        }

        return patches;
    }

    // -------------------------------------------------------------------------
    // Patch application
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies patches to a text field. All patches must target the same
    /// source + field combination. Applied in reverse position order so that
    /// earlier indices remain valid after each substitution.
    /// </summary>
    public static string ApplyPatches(string text, IReadOnlyList<RenamePatch> patches)
    {
        if (patches.Count == 0) return text;

        // Sort descending by position so we apply from the end of the string first
        var sorted = patches
            .OrderByDescending(p => p.Position)
            .ToList();

        foreach (var patch in sorted)
        {
            var pos = patch.Position;
            var len = patch.OriginalLength;
            if (pos < 0 || pos + len > text.Length) continue;
            text = text[..pos] + patch.Replacement + text[(pos + len)..];
        }

        return text;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static void ScanTextField(
        string? text,
        string sourceId,
        string sourceName,
        MatchSource source,
        string field,
        string searchTerm,
        List<RenameMatch> results)
    {
        if (string.IsNullOrEmpty(text)) return;

        var comparison = StringComparison.OrdinalIgnoreCase;
        var start = 0;

        while (true)
        {
            var idx = text.IndexOf(searchTerm, start, comparison);
            if (idx < 0) break;

            var before = ExtractContext(text, idx, leading: true);
            var after  = ExtractContext(text, idx + searchTerm.Length, leading: false);
            var matched = text.Substring(idx, searchTerm.Length);

            results.Add(new RenameMatch(
                SourceId:      sourceId,
                SourceName:    sourceName,
                Source:        source,
                Field:         field,
                Type:          MatchType.FullName,
                MatchedText:   matched,
                Position:      idx,
                ContextBefore: before,
                ContextAfter:  after));

            start = idx + searchTerm.Length;
        }
    }

    /// <summary>Extracts up to <see cref="ContextRadius"/> characters before or after a position.</summary>
    private static string ExtractContext(string text, int position, bool leading)
    {
        if (leading)
        {
            var start = Math.Max(0, position - ContextRadius);
            return text[start..position];
        }
        else
        {
            var end = Math.Min(text.Length, position + ContextRadius);
            return text[position..end];
        }
    }

    /// <summary>
    /// Echoes the casing style of <paramref name="original"/> onto <paramref name="replacement"/>.
    /// All-uppercase → uppercase; all-lowercase → lowercase; mixed → as-is.
    /// </summary>
    private static string PreserveCase(string original, string replacement)
    {
        if (string.IsNullOrEmpty(original)) return replacement;

        var hasLetters = original.Any(char.IsLetter);
        if (!hasLetters) return replacement;

        var allUpper = original.All(c => !char.IsLetter(c) || char.IsUpper(c));
        var allLower = original.All(c => !char.IsLetter(c) || char.IsLower(c));

        if (allUpper) return replacement.ToUpperInvariant();
        if (allLower) return replacement.ToLowerInvariant();
        return replacement;
    }
}
