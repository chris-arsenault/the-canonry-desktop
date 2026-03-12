namespace TheCanonry.Persistence.Entities;

public class SimulationSlotEntity
{
    public long Id { get; set; }
    public required string ProjectId { get; set; }
    public required int SlotIndex { get; set; }
    public required string SimulationRunId { get; set; }
    public int? FinalTick { get; set; }
    public string? FinalEraId { get; set; }
    public string? Label { get; set; }
    public bool IsTemporary { get; set; }
    public required DateTime UpdatedAt { get; set; }
}
