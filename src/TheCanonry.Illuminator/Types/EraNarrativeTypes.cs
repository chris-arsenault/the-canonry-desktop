namespace TheCanonry.Illuminator.Types;

/// <summary>
/// Tone presets for era narratives (distinct from annotation tones — tuned for long-form).
/// </summary>
public enum EraNarrativeTone
{
    Witty,
    Cantankerous,
    Bemused,
    Defiant,
    Sardonic,
    Tender,
    Hopeful,
    Enthusiastic,
}

/// <summary>
/// Steps in the era narrative pipeline.
/// </summary>
public enum EraNarrativeStep
{
    Threads,
    Generate,
    Edit,
}

/// <summary>
/// Status of an era narrative through its lifecycle.
/// </summary>
public enum EraNarrativeStatus
{
    Pending,
    Generating,
    StepComplete,
    Complete,
    Cancelled,
    Failed,
}

/// <summary>
/// A thematic thread identified during era narrative synthesis.
/// </summary>
public sealed record EraNarrativeThread
{
    public required string ThreadId { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<string> CulturalActors { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> ChronicleIds { get; init; }
    public required string Arc { get; init; }
    public string? Register { get; init; }
    public string? Material { get; init; }
}

// =============================================================================
// Era Narrative Image Refs
// =============================================================================

/// <summary>
/// Display size hint for era narrative inline images.
/// </summary>
public enum EraNarrativeImageSize
{
    Small,
    Medium,
    Large,
    FullWidth,
}

/// <summary>
/// Base type for era narrative inline image references.
/// Either a reference to an existing chronicle image or a new scene request.
/// </summary>
public abstract record EraNarrativeImageRef
{
    public required string RefId { get; init; }
    public required string AnchorText { get; init; }
    public int? AnchorIndex { get; init; }
    public required EraNarrativeImageSize Size { get; init; }
    public string? Justification { get; init; }
    public string? Caption { get; init; }
}

/// <summary>
/// Reference to an existing chronicle image (cover or scene image from a chronicle).
/// </summary>
public sealed record EraNarrativeChronicleImageRef : EraNarrativeImageRef
{
    public required string ChronicleId { get; init; }
    public required string ChronicleTitle { get; init; }
    public required string ImageSource { get; init; }
    public string? ImageRefId { get; init; }
    public required string ImageId { get; init; }
}

/// <summary>
/// New scene image generated specifically for the era narrative.
/// </summary>
public sealed record EraNarrativePromptRequestRef : EraNarrativeImageRef
{
    public required string SceneDescription { get; init; }
    public required string Status { get; init; }
    public string? GeneratedImageId { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Container for era narrative inline image refs with generation metadata.
/// </summary>
public sealed record EraNarrativeImageRefs(
    IReadOnlyList<EraNarrativeImageRef> Refs,
    long GeneratedAt,
    string Model);

// =============================================================================
// Era Narrative Quotes & Strategic Dynamics
// =============================================================================

/// <summary>
/// In-world text that exists as a cultural artifact — quotable as primary source.
/// </summary>
public sealed record EraNarrativeQuote(string Text, string Origin, string Context);

/// <summary>
/// Strategic interaction between cultures inferred by the historian.
/// </summary>
public sealed record EraNarrativeStrategicDynamic(
    string Interaction,
    IReadOnlyList<string> Actors,
    string Dynamic);

// =============================================================================
// Thread Synthesis
// =============================================================================

/// <summary>
/// Thread synthesis output — the structural plan for an era narrative.
/// </summary>
public sealed record EraNarrativeThreadSynthesis
{
    public required IReadOnlyList<EraNarrativeThread> Threads { get; init; }
    public required string Thesis { get; init; }
    public string? Counterweight { get; init; }
    public IReadOnlyList<EraNarrativeQuote>? Quotes { get; init; }
    public IReadOnlyList<EraNarrativeStrategicDynamic>? StrategicDynamics { get; init; }
    public required long GeneratedAt { get; init; }
    public required string Model { get; init; }
    public required int InputTokens { get; init; }
    public required int OutputTokens { get; init; }
    public required decimal ActualCost { get; init; }
}

/// <summary>
/// Persisted era narrative record — full state of an era narrative.
/// </summary>
public sealed class EraNarrative
{
    public required string Id { get; init; }
    public required string EraId { get; init; }
    public required string EraName { get; init; }
    public required EraNarrativeStatus Status { get; set; }
    public required EraNarrativeTone Tone { get; init; }
    public required EraNarrativeStep CurrentStep { get; set; }

    public EraNarrativeThreadSynthesis? ThreadSynthesis { get; set; }
    public string? Content { get; set; }
    public string? Summary { get; set; }
    public string? CoverSceneDescription { get; set; }
    public string? CoverImageId { get; set; }

    /// <summary>
    /// Inline image refs (chronicle images + generated scenes).
    /// </summary>
    public EraNarrativeImageRefs? ImageRefs { get; set; }

    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public decimal TotalActualCost { get; set; }

    public required long CreatedAt { get; init; }
    public long UpdatedAt { get; set; }
}
