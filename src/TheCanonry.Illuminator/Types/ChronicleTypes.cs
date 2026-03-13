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

// =============================================================================
// Cover Image
// =============================================================================

/// <summary>
/// Generation state for a chronicle cover image.
/// </summary>
public enum CoverImageStatus
{
    Pending,
    Generating,
    Complete,
    Failed,
}

/// <summary>
/// Cover image metadata for a chronicle — LLM-generated scene description
/// with style assignments and generation state.
/// </summary>
public sealed record ChronicleCoverImage
{
    public required string SceneDescription { get; init; }
    public required IReadOnlyList<string> InvolvedEntityIds { get; init; }
    public required CoverImageStatus Status { get; init; }
    public string? GeneratedImageId { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<string>? VisualTags { get; init; }
    public string? SuggestedArtisticStyleId { get; init; }
    public string? SuggestedCompositionStyleId { get; init; }
    public string? SuggestedColorPaletteId { get; init; }
    public IReadOnlyList<string>? RankedArtisticStyleIds { get; init; }
    public IReadOnlyList<string>? RankedCompositionStyleIds { get; init; }
    public IReadOnlyList<string>? RankedColorPaletteIds { get; init; }
    public string? SecondaryArtisticStyleId { get; init; }
    public string? SecondaryCompositionStyleId { get; init; }
    public string? SecondaryColorPaletteId { get; init; }
}

// =============================================================================
// Tertiary Cast
// =============================================================================

/// <summary>
/// An entity mentioned in chronicle text but not in the declared cast.
/// Detected by text analysis, user-confirmable.
/// </summary>
public sealed record TertiaryCastEntry
{
    public required string EntityId { get; init; }
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required string MatchedAs { get; init; }
    public int? MatchStart { get; init; }
    public int? MatchEnd { get; init; }
    public required bool Accepted { get; init; }
}

// =============================================================================
// Cohesion Report
// =============================================================================

/// <summary>
/// A single cohesion check result.
/// </summary>
public sealed record CohesionCheck(bool Pass, string Notes);

/// <summary>
/// A section-level goal check result.
/// </summary>
public sealed record SectionGoalCheck(string SectionId, bool Pass, string Notes);

/// <summary>
/// Severity level for cohesion issues.
/// </summary>
public enum CohesionIssueSeverity
{
    Critical,
    Minor,
}

/// <summary>
/// A specific cohesion issue identified during validation.
/// </summary>
public sealed record CohesionIssue
{
    public required CohesionIssueSeverity Severity { get; init; }
    public string? SectionId { get; init; }
    public required string CheckType { get; init; }
    public required string Description { get; init; }
    public required string Suggestion { get; init; }
}

/// <summary>
/// Structured container for the six cohesion checks.
/// </summary>
public sealed record CohesionChecks
{
    public required CohesionCheck PlotStructure { get; init; }
    public required CohesionCheck EntityConsistency { get; init; }
    public required IReadOnlyList<SectionGoalCheck> SectionGoals { get; init; }
    public required CohesionCheck Resolution { get; init; }
    public required CohesionCheck FactualAccuracy { get; init; }
    public required CohesionCheck ThemeExpression { get; init; }
}

/// <summary>
/// Full cohesion validation report — 6 checks, issues, and overall score.
/// </summary>
public sealed record CohesionReport
{
    public required int OverallScore { get; init; }
    public required CohesionChecks Checks { get; init; }
    public required IReadOnlyList<CohesionIssue> Issues { get; init; }
    public long? GeneratedAt { get; init; }
    public string? Model { get; init; }
}

// =============================================================================
// Quick Check Report
// =============================================================================

/// <summary>
/// Confidence level for a quick-check suspect.
/// </summary>
public enum QuickCheckConfidence
{
    High,
    Medium,
    Low,
}

/// <summary>
/// Overall assessment of quick-check results.
/// </summary>
public enum QuickCheckAssessment
{
    Clean,
    Minor,
    Flagged,
}

/// <summary>
/// A suspicious unanchored reference detected by quick check.
/// </summary>
public sealed record QuickCheckSuspect
{
    public required string Phrase { get; init; }
    public required string Context { get; init; }
    public required string Reasoning { get; init; }
    public required QuickCheckConfidence Confidence { get; init; }
}

/// <summary>
/// Fast unanchored-reference scan report.
/// </summary>
public sealed record QuickCheckReport
{
    public required IReadOnlyList<QuickCheckSuspect> Suspects { get; init; }
    public required QuickCheckAssessment Assessment { get; init; }
    public required string Summary { get; init; }
}

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
    public string? NarrativeStyleName { get; init; }
    public string? CraftPosture { get; init; }
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
    public string? AcceptedVersionId { get; set; }

    // Generation prompts (the actual prompts sent to LLM)
    public string? GenerationSystemPrompt { get; set; }
    public string? GenerationUserPrompt { get; set; }

    // Refinements
    public string? Summary { get; set; }
    public ChronicleImageRefs? ImageRefs { get; set; }
    public string? NarrativeDirection { get; init; }

    // Cover image
    public ChronicleCoverImage? CoverImage { get; set; }
    public long? CoverImageGeneratedAt { get; set; }
    public string? CoverImageModel { get; set; }

    // Cohesion validation
    public CohesionReport? CohesionReport { get; set; }
    public long? ValidatedAt { get; set; }
    public bool ValidationStale { get; set; }

    // Version comparison (user-triggered)
    public string? ComparisonReport { get; set; }
    public long? ComparisonReportGeneratedAt { get; set; }
    public string? CombineInstructions { get; set; }

    // Temporal alignment check (user-triggered)
    public string? TemporalCheckReport { get; set; }
    public long? TemporalCheckReportGeneratedAt { get; set; }

    // Quick check — unanchored entity reference scan (user-triggered)
    public QuickCheckReport? QuickCheckReport { get; set; }
    public long? QuickCheckReportGeneratedAt { get; set; }

    // Tertiary cast — entities detected in text but not in declared cast
    public IReadOnlyList<TertiaryCastEntry>? TertiaryCast { get; set; }
    public long? TertiaryCastDetectedAt { get; set; }

    // Revision tracking
    public int EditVersion { get; set; }

    // Final content
    public string? FinalContent { get; set; }
    public long? AcceptedAt { get; set; }

    // Historian-assigned chronological year
    public int? EraYear { get; set; }
    public string? EraYearReasoning { get; set; }

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
