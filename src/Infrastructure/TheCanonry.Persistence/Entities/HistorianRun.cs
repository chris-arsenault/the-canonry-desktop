namespace TheCanonry.Persistence.Entities;

public class HistorianRun
{
    public long Id { get; set; }
    public string SimulationRunId { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Notes { get; set; } = "[]"; // JSON array of HistorianNote
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
