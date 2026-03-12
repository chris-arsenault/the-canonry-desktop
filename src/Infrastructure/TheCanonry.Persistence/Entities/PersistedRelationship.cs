namespace TheCanonry.Persistence.Entities;

public class PersistedRelationship
{
    public long Id { get; set; }
    public required string SimulationRunId { get; set; }
    public required string SourceId { get; set; }
    public required string TargetId { get; set; }
    public required string Kind { get; set; }
    public required double Strength { get; set; }
    public required double Distance { get; set; }
    public required string Category { get; set; }
    public required string Status { get; set; }
    public required int CreatedAtTick { get; set; }
}
