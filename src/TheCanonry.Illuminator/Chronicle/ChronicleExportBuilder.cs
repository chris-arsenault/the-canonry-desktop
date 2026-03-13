using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Chronicle;

// =============================================================================
// Export Types
// =============================================================================

public sealed record ChronicleExportIdentity
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Format { get; init; }
    public required string FocusType { get; init; }
    public required string NarrativeStyleId { get; init; }
    public string? NarrativeStyleName { get; init; }
    public string? CraftPosture { get; init; }
    public ChronicleExportLens? Lens { get; init; }
    public string? NarrativeDirection { get; init; }
    public required string CreatedAt { get; init; }
    public string? AcceptedAt { get; init; }
    public required string Model { get; init; }
}

public sealed record ChronicleExportLens(string EntityId, string EntityName, string EntityKind);

public sealed record ChronicleExportLlmCall(string SystemPrompt, string UserPrompt, string Model);

public sealed record ChronicleExportVersion
{
    public required string VersionId { get; init; }
    public required string GeneratedAt { get; init; }
    public string? Sampling { get; init; }
    public string? Step { get; init; }
    public required string Model { get; init; }
    public required int WordCount { get; init; }
    public required string Content { get; init; }
    public required string SystemPrompt { get; init; }
    public required string UserPrompt { get; init; }
}

public sealed record ChronicleExportCoverImage
{
    public required string SceneDescription { get; init; }
    public required IReadOnlyList<string> InvolvedEntityIds { get; init; }
    public required string Status { get; init; }
    public string? GeneratedImageId { get; init; }
}

/// <summary>
/// Full chronicle export structure matching TS ChronicleExport v1.3.
/// </summary>
public sealed record ChronicleExport
{
    [JsonPropertyName("exportVersion")]
    public string ExportVersion { get; init; } = "1.3";

    public required string ExportedAt { get; init; }
    public string? ActiveVersionId { get; init; }
    public string? AcceptedVersionId { get; init; }

    public required ChronicleExportIdentity Chronicle { get; init; }

    public required string Content { get; init; }
    public required int WordCount { get; init; }

    public required ChronicleExportLlmCall GenerationLlmCall { get; init; }

    public IReadOnlyList<ChronicleExportVersion>? Versions { get; init; }
    public string? Summary { get; init; }
    public ChronicleExportCoverImage? CoverImage { get; init; }
    public string? ComparisonReport { get; init; }
    public string? CombineInstructions { get; init; }
    public string? TemporalCheckReport { get; init; }
    public int? EraYear { get; init; }
    public string? EraYearReasoning { get; init; }
}

// =============================================================================
// Builder
// =============================================================================

/// <summary>
/// Builds a structured JSON export from a ChronicleRecord using only stored data.
/// Ported from apps/illuminator/webui/src/lib/chronicleExport.ts.
/// </summary>
public static class ChronicleExportBuilder
{
    private static readonly Regex WordSplitPattern = new(@"\s+", RegexOptions.Compiled);

    public static ChronicleExport Build(ChronicleRecord chronicle)
    {
        var versions = (chronicle.GenerationHistory ?? [])
            .OrderBy(v => v.GeneratedAt)
            .ToList();

        var latestVersion = versions.Count > 0
            ? versions.MaxBy(v => v.GeneratedAt)
            : null;

        var activeVersionId = chronicle.ActiveVersionId ?? latestVersion?.VersionId;
        var isAccepted = chronicle.AcceptedAt.HasValue && chronicle.FinalContent is not null;
        var acceptedVersionId = chronicle.AcceptedVersionId
            ?? (isAccepted ? activeVersionId : null);
        var effectiveVersionId = isAccepted
            ? acceptedVersionId ?? activeVersionId
            : activeVersionId;

        var currentContent = chronicle.AssembledContent ?? chronicle.FinalContent ?? "";
        var historyMatch = versions.FirstOrDefault(v => v.VersionId == effectiveVersionId);

        string content;
        int wordCount;
        string systemPrompt;
        string userPrompt;
        string model;

        if (historyMatch is not null)
        {
            content = isAccepted && chronicle.FinalContent is not null
                ? chronicle.FinalContent
                : historyMatch.Content;
            wordCount = CountWords(content);
            systemPrompt = historyMatch.SystemPrompt;
            userPrompt = historyMatch.UserPrompt;
            model = historyMatch.Model;
        }
        else
        {
            content = isAccepted && chronicle.FinalContent is not null
                ? chronicle.FinalContent
                : currentContent;
            wordCount = CountWords(content);
            systemPrompt = chronicle.GenerationSystemPrompt
                ?? "(prompt not stored)";
            userPrompt = chronicle.GenerationUserPrompt
                ?? "(prompt not stored)";
            model = chronicle.Model;
        }

        var export = new ChronicleExport
        {
            ExportedAt = DateTimeOffset.UtcNow.ToString("O"),
            ActiveVersionId = activeVersionId,
            AcceptedVersionId = acceptedVersionId,

            Chronicle = new ChronicleExportIdentity
            {
                Id = chronicle.ChronicleId,
                Title = chronicle.Title,
                Format = chronicle.Format.ToString().ToLowerInvariant(),
                FocusType = chronicle.FocusType.ToString().ToLowerInvariant(),
                NarrativeStyleId = chronicle.NarrativeStyleId,
                NarrativeStyleName = chronicle.NarrativeStyleName,
                CraftPosture = chronicle.CraftPosture,
                Lens = chronicle.Lens is not null
                    ? new ChronicleExportLens(chronicle.Lens.EntityId, chronicle.Lens.EntityName, chronicle.Lens.EntityKind)
                    : null,
                NarrativeDirection = chronicle.NarrativeDirection,
                CreatedAt = FormatTimestamp(chronicle.CreatedAt),
                AcceptedAt = chronicle.AcceptedAt.HasValue ? FormatTimestamp(chronicle.AcceptedAt.Value) : null,
                Model = chronicle.Model,
            },

            Content = content,
            WordCount = wordCount,

            GenerationLlmCall = new ChronicleExportLlmCall(systemPrompt, userPrompt, model),

            Versions = versions.Select(v => new ChronicleExportVersion
            {
                VersionId = v.VersionId,
                GeneratedAt = FormatTimestamp(v.GeneratedAt),
                Sampling = v.Sampling,
                Step = v.Step?.ToString().ToLowerInvariant(),
                Model = v.Model,
                WordCount = v.WordCount,
                Content = v.Content,
                SystemPrompt = v.SystemPrompt,
                UserPrompt = v.UserPrompt,
            }).ToList(),

            Summary = chronicle.Summary,

            CoverImage = chronicle.CoverImage is not null
                ? new ChronicleExportCoverImage
                {
                    SceneDescription = chronicle.CoverImage.SceneDescription,
                    InvolvedEntityIds = chronicle.CoverImage.InvolvedEntityIds,
                    Status = chronicle.CoverImage.Status.ToString().ToLowerInvariant(),
                    GeneratedImageId = chronicle.CoverImage.GeneratedImageId,
                }
                : null,

            ComparisonReport = chronicle.ComparisonReport,
            CombineInstructions = chronicle.CombineInstructions,
            TemporalCheckReport = chronicle.TemporalCheckReport,
            EraYear = chronicle.EraYear,
            EraYearReasoning = chronicle.EraYearReasoning,
        };

        return export;
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : WordSplitPattern.Split(text.Trim()).Length;

    private static string FormatTimestamp(long epochMs) =>
        DateTimeOffset.FromUnixTimeMilliseconds(epochMs).ToString("O");
}
