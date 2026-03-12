namespace TheCanonry.Illuminator.Types;

/// <summary>
/// Tone presets for historian annotations.
/// </summary>
public enum HistorianTone
{
    Scholarly,
    Witty,
    Weary,
    Forensic,
    Elegiac,
    Cantankerous,
    Rueful,
    Conspiratorial,
    Bemused,
}

/// <summary>
/// Type classification for historian notes.
/// </summary>
public enum HistorianNoteType
{
    Commentary,
    Correction,
    Tangent,
    Skepticism,
    Pedantic,
    Temporal,
}

/// <summary>
/// Project-level historian persona configuration.
/// </summary>
public sealed record HistorianConfig
{
    public required string Name { get; init; }
    public required string Background { get; init; }
    public required IReadOnlyList<string> PersonalityTraits { get; init; }
    public required IReadOnlyList<string> Biases { get; init; }
    public required string Stance { get; init; }
    public required IReadOnlyList<string> PrivateFacts { get; init; }
    public required IReadOnlyList<string> RunningGags { get; init; }
}

/// <summary>
/// A single historian annotation anchored to source text.
/// </summary>
public sealed record HistorianNote
{
    public required string NoteId { get; init; }
    public required string AnchorPhrase { get; init; }
    public required string Text { get; init; }
    public required HistorianNoteType Type { get; init; }
    public required HistorianTone Tone { get; init; }
}

/// <summary>
/// Status of a historian review run.
/// </summary>
public enum HistorianRunStatus
{
    Pending,
    Generating,
    Reviewing,
    Complete,
    Cancelled,
    Failed,
}

/// <summary>
/// A historian review run — tracks an annotation session.
/// </summary>
public sealed class HistorianRun
{
    public required string Id { get; init; }
    public required string EntityId { get; init; }
    public required IReadOnlyList<HistorianNote> Notes { get; set; }
    public required HistorianRunStatus Status { get; set; }
    public required HistorianTone Tone { get; init; }
    public string? Error { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal ActualCost { get; set; }
    public required long CreatedAt { get; init; }
    public long UpdatedAt { get; set; }
}
