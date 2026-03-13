using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Chronicle;

// =============================================================================
// Annotation Review Export Types
// =============================================================================

/// <summary>
/// A single annotation in the review export.
/// </summary>
public sealed record AnnotationExportNote(
    string Type,
    string AnchorPhrase,
    string Text);

/// <summary>
/// A chronicle row in the annotation review export.
/// </summary>
public sealed record AnnotationExportRow
{
    public required string Title { get; init; }
    public required string Format { get; init; }
    public string? NarrativeStyleName { get; init; }
    public string? AssignedTone { get; init; }
    public required int NoteCount { get; init; }
    public required IReadOnlyList<AnnotationExportNote> Annotations { get; init; }
}

/// <summary>
/// Full annotation review export — all chronicled annotations for bulk review.
/// </summary>
public sealed record AnnotationReviewExport
{
    public required string ExportedAt { get; init; }
    public required int TotalChronicles { get; init; }
    public required int TotalAnnotations { get; init; }
    public required IReadOnlyList<AnnotationExportRow> Chronicles { get; init; }
}

// =============================================================================
// Tone Review Export Types
// =============================================================================

/// <summary>
/// A note range (min/max historian annotations for a chronicle).
/// </summary>
public sealed record NoteRange(int Min, int Max);

/// <summary>
/// Fact coverage entry in the tone review export.
/// </summary>
public sealed record ToneExportFactCoverageEntry(
    string FactId,
    string Rating,
    bool WasFaceted);

/// <summary>
/// Annotation guidance entry for the tone review export.
/// </summary>
public sealed record ToneExportGuidanceEntry(
    string FactId,
    string Action,
    string? Evidence,
    string? CorpusStrength);

/// <summary>
/// Annotation guidance (required vs optional fact targets) in the tone review export.
/// </summary>
public sealed record ToneExportAnnotationGuidance(
    IReadOnlyList<ToneExportGuidanceEntry> Required,
    IReadOnlyList<ToneExportGuidanceEntry> Optional);

/// <summary>
/// Tone ranking in the export.
/// </summary>
public sealed record ToneExportRanking(
    IReadOnlyList<string> Ranking,
    IReadOnlyDictionary<string, string>? Rationales);

/// <summary>
/// A chronicle row in the tone review export.
/// </summary>
public sealed record ToneExportRow
{
    public required string Title { get; init; }
    public string? Summary { get; init; }
    public string? Brief { get; init; }
    public required int WordCount { get; init; }
    public required NoteRange NoteRange { get; init; }
    public string? AssignedTone { get; init; }
    public ToneExportRanking? ToneRanking { get; init; }
    public IReadOnlyList<ToneExportFactCoverageEntry>? FactCoverage { get; init; }
    public required ToneExportAnnotationGuidance AnnotationGuidance { get; init; }
}

/// <summary>
/// Full tone review export — tone assignments and fact coverage guidance.
/// </summary>
public sealed record ToneReviewExport
{
    public required string ExportedAt { get; init; }
    public required int TotalChronicles { get; init; }
    public required IReadOnlyList<ToneExportRow> Chronicles { get; init; }
}

// =============================================================================
// Input Types — callers project their data into these shapes
// =============================================================================

/// <summary>
/// Input for annotation review export. Callers build this from their data layer.
/// </summary>
public sealed record AnnotationExportInput
{
    public required string Title { get; init; }
    public required string Format { get; init; }
    public string? NarrativeStyleName { get; init; }
    public string? AssignedTone { get; init; }
    public required IReadOnlyList<HistorianNote> Notes { get; init; }
}

/// <summary>
/// Input for tone review export. Callers build this from their data layer.
/// </summary>
public sealed record ToneExportInput
{
    public required string Title { get; init; }
    public string? Summary { get; init; }
    public string? Brief { get; init; }
    public required int WordCount { get; init; }
    public string? AssignedTone { get; init; }
    public IReadOnlyList<string>? ToneRankingValues { get; init; }
    public IReadOnlyDictionary<string, string>? ToneRankingRationales { get; init; }
    public IReadOnlyList<ToneExportFactCoverageEntry>? FactCoverage { get; init; }
    public IReadOnlyList<ToneExportGuidanceEntry>? GuidanceTargets { get; init; }
}

// =============================================================================
// Builder
// =============================================================================

/// <summary>
/// Builds structured JSON exports for bulk annotation review and tone review.
/// Ported from apps/illuminator/webui/src/lib/chronicleExport.ts
/// (downloadBulkAnnotationReviewExport + downloadBulkToneReviewExport).
/// </summary>
public static class AnnotationExportBuilder
{
    /// <summary>
    /// Build annotation review export — chronicles with their historian notes.
    /// Only includes chronicles that have at least one note.
    /// </summary>
    public static AnnotationReviewExport BuildAnnotationReview(
        IReadOnlyList<AnnotationExportInput> chronicles)
    {
        var rows = chronicles
            .Where(c => c.Notes.Count > 0)
            .OrderBy(c => c.Title, StringComparer.Ordinal)
            .Select(c => new AnnotationExportRow
            {
                Title = c.Title,
                Format = c.Format,
                NarrativeStyleName = c.NarrativeStyleName,
                AssignedTone = c.AssignedTone,
                NoteCount = c.Notes.Count,
                Annotations = c.Notes.Select(n => new AnnotationExportNote(
                    n.Type.ToString().ToLowerInvariant(),
                    n.AnchorPhrase,
                    n.Text)).ToList(),
            })
            .ToList();

        return new AnnotationReviewExport
        {
            ExportedAt = DateTimeOffset.UtcNow.ToString("O"),
            TotalChronicles = rows.Count,
            TotalAnnotations = rows.Sum(r => r.NoteCount),
            Chronicles = rows,
        };
    }

    /// <summary>
    /// Build tone review export — tone assignments, note ranges, and fact coverage guidance.
    /// </summary>
    public static ToneReviewExport BuildToneReview(
        IReadOnlyList<ToneExportInput> chronicles)
    {
        var rows = chronicles
            .Select(c =>
            {
                var noteRange = Enrichment.Prompts.HistorianPrompts.ComputeNoteRange("chronicle", c.WordCount);

                // Split guidance into required vs optional (same logic as TS)
                var allTargets = c.GuidanceTargets ?? [];
                var maxRequired = noteRange.Max <= 4 ? 1 : allTargets.Count;
                var required = allTargets.Take(maxRequired).ToList();
                var optional = allTargets.Skip(maxRequired).ToList();

                return new ToneExportRow
                {
                    Title = c.Title,
                    Summary = c.Summary,
                    Brief = c.Brief,
                    WordCount = c.WordCount,
                    NoteRange = new NoteRange(noteRange.Min, noteRange.Max),
                    AssignedTone = c.AssignedTone,
                    ToneRanking = c.ToneRankingValues is { Count: > 0 }
                        ? new ToneExportRanking(c.ToneRankingValues, c.ToneRankingRationales)
                        : null,
                    FactCoverage = c.FactCoverage,
                    AnnotationGuidance = new ToneExportAnnotationGuidance(required, optional),
                };
            })
            .ToList();

        return new ToneReviewExport
        {
            ExportedAt = DateTimeOffset.UtcNow.ToString("O"),
            TotalChronicles = rows.Count,
            Chronicles = rows,
        };
    }
}
