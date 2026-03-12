using TheCanonry.Schema.Ids;

namespace TheCanonry.Illuminator.Types;

/// <summary>
/// Narrative format for chronicles: story-form or document-form.
/// </summary>
public enum ChronicleFormat
{
    Story,
    Document,
}

/// <summary>
/// Status of a chronicle through its generation lifecycle.
/// </summary>
public enum ChronicleStatus
{
    NotStarted,
    Generating,
    AssemblyReady,
    Editing,
    Validating,
    ValidationReady,
    Failed,
    Complete,
}

/// <summary>
/// Which pipeline step produced a particular chronicle version.
/// </summary>
public enum VersionStep
{
    Generate,
    Regenerate,
    Creative,
    Combine,
    CopyEdit,
    PerspectiveSynthesis,
    ImageRefs,
}

/// <summary>
/// Focus mode for chronicle generation.
/// </summary>
public enum ChronicleFocusType
{
    Single,
    Ensemble,
}

/// <summary>
/// A role assignment mapping an entity to a narrative role.
/// </summary>
public sealed record ChronicleRoleAssignment(
    string Role,
    string EntityId,
    string EntityName,
    string EntityKind,
    bool IsPrimary);

/// <summary>
/// A narrative lens entity that provides contextual framing (not a cast member).
/// </summary>
public sealed record NarrativeLens(
    string EntityId,
    string EntityName,
    string EntityKind);

/// <summary>
/// Image reference in a chronicle — either a reference to an existing entity
/// image or a request for a new prompt-generated image.
/// </summary>
public abstract record ChronicleImageRef
{
    public required string RefId { get; init; }
    public required string AnchorText { get; init; }
    public int? AnchorIndex { get; init; }
    public required ChronicleImageSize Size { get; init; }
    public string? Justification { get; init; }
    public string? Caption { get; init; }
}

/// <summary>
/// Reference to an existing entity image.
/// </summary>
public sealed record EntityImageRef : ChronicleImageRef
{
    public required string EntityId { get; init; }
}

/// <summary>
/// Request for a new prompt-generated image.
/// </summary>
public sealed record PromptRequestRef : ChronicleImageRef
{
    public required string SceneDescription { get; init; }
    public IReadOnlyList<string>? InvolvedEntityIds { get; init; }
    public required string Status { get; init; }
    public string? GeneratedImageId { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<string>? VisualTags { get; init; }
    public string? SuggestedArtisticStyleId { get; init; }
    public string? SuggestedCompositionStyleId { get; init; }
    public string? SuggestedColorPaletteId { get; init; }
}

/// <summary>
/// Display size hint for chronicle images.
/// </summary>
public enum ChronicleImageSize
{
    Small,
    Medium,
    Large,
    FullWidth,
}

/// <summary>
/// Structured image refs stored with a chronicle.
/// </summary>
public sealed record ChronicleImageRefs(
    IReadOnlyList<ChronicleImageRef> Refs,
    long GeneratedAt,
    string Model);

/// <summary>
/// Snapshot of a chronicle generation version.
/// </summary>
public sealed record ChronicleVersion
{
    public required string VersionId { get; init; }
    public required long GeneratedAt { get; init; }
    public required string Content { get; init; }
    public required int WordCount { get; init; }
    public required string Model { get; init; }
    public string? Sampling { get; init; }
    public required string SystemPrompt { get; init; }
    public required string UserPrompt { get; init; }
    public VersionStep? Step { get; init; }
    public decimal? EstimatedCost { get; init; }
    public decimal? ActualCost { get; init; }
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
}

/// <summary>
/// Persisted chronicle record — the full state of a chronicle through its lifecycle.
/// </summary>
public sealed class ChronicleRecord
{
    public required string ChronicleId { get; init; }
    public required string ProjectId { get; init; }
    public required string SimulationRunId { get; init; }

    // Chronicle identity
    public required string Title { get; set; }
    public required ChronicleFormat Format { get; init; }
    public required ChronicleFocusType FocusType { get; init; }
    public required IReadOnlyList<ChronicleRoleAssignment> RoleAssignments { get; init; }
    public NarrativeLens? Lens { get; init; }
    public required string NarrativeStyleId { get; init; }
    public required IReadOnlyList<string> SelectedEntityIds { get; init; }
    public required IReadOnlyList<string> SelectedEventIds { get; init; }
    public required IReadOnlyList<string> SelectedRelationshipIds { get; init; }
    public string? EntrypointId { get; init; }

    // Generation state
    public required ChronicleStatus Status { get; set; }
    public string? FailureStep { get; set; }
    public string? FailureReason { get; set; }

    // Content
    public string? AssembledContent { get; set; }
    public long? AssembledAt { get; set; }
    public IReadOnlyList<ChronicleVersion>? GenerationHistory { get; set; }
    public string? ActiveVersionId { get; set; }

    // Refinements
    public string? Summary { get; set; }
    public ChronicleImageRefs? ImageRefs { get; set; }
    public string? NarrativeDirection { get; init; }

    // Revision tracking
    public int EditVersion { get; set; }

    // Final content
    public string? FinalContent { get; set; }
    public long? AcceptedAt { get; set; }

    // Cost tracking (aggregated across all LLM calls)
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalActualCost { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }

    // Metadata
    public required string Model { get; init; }
    public required long CreatedAt { get; init; }
    public long UpdatedAt { get; set; }
}
