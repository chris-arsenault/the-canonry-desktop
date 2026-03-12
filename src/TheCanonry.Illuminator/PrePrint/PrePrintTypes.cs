namespace TheCanonry.Illuminator.PrePrint;

// =============================================================================
// Content Tree
// =============================================================================

public enum ContentNodeType { Folder, Entity, Chronicle, StaticPage, EraNarrative }

public sealed class ContentTreeNode
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required ContentNodeType Type { get; init; }
    public string? ContentId { get; init; }
    public List<ContentTreeNode>? Children { get; set; }
}

public sealed record FlattenedNode(string Id, string Name, ContentNodeType Type, string? ContentId, string Path, int Depth);

// =============================================================================
// Stats
// =============================================================================

public sealed record PrePrintStats(
    WordCountBreakdown WordCounts,
    ImageStats Images,
    CompletenessStats Completeness);

public sealed record WordCountBreakdown(
    int TotalWords,
    int EntityWords,
    int ChronicleWords,
    int EraNarrativeWords,
    int StaticPageWords,
    int HistorianWords);

public sealed record ImageStats(
    int TotalImages,
    IReadOnlyDictionary<string, int> ByAspect,
    IReadOnlyDictionary<string, int> ByType);

public sealed record CompletenessStats(
    int TotalEntities,
    int EntitiesWithDescription,
    int TotalChronicles,
    int AcceptedChronicles,
    int TotalStaticPages,
    int PublishedStaticPages,
    int TotalEraNarratives,
    int CompletedEraNarratives);

// =============================================================================
// Export
// =============================================================================

public sealed record ExportManifest(
    DateTime GeneratedAt,
    int TotalFiles,
    int TotalImages,
    IReadOnlyList<string> FilePaths);
