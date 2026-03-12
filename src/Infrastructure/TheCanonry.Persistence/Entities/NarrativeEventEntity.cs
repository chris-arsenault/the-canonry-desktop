namespace TheCanonry.Persistence.Entities;

public class NarrativeEventEntity
{
    public long Id { get; set; }
    public string SimulationRunId { get; set; } = "";
    public int Tick { get; set; }
    public string EraId { get; set; } = "";
    public string EventKind { get; set; } = ""; // "entity_created", "relationship_formed", etc.
    public double Significance { get; set; }
    public string SubjectId { get; set; } = ""; // Primary entity involved
    public string Action { get; set; } = "";
    public string Description { get; set; } = "";
    public string CausedByJson { get; set; } = "{}"; // JSON: EventCause
    public string NarrativeTagsJson { get; set; } = "[]"; // JSON: string[]
    public string ParticipantEffectsJson { get; set; } = "[]"; // JSON: ParticipantEffect[]
}
