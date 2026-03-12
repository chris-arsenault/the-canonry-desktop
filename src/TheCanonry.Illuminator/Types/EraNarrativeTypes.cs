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

/// <summary>
/// Thread synthesis output — the structural plan for an era narrative.
/// </summary>
public sealed record EraNarrativeThreadSynthesis
{
    public required IReadOnlyList<EraNarrativeThread> Threads { get; init; }
    public required string Thesis { get; init; }
    public string? Counterweight { get; init; }
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

    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public decimal TotalActualCost { get; set; }

    public required long CreatedAt { get; init; }
    public long UpdatedAt { get; set; }
}
