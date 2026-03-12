namespace TheCanonry.Persistence.Entities;

public class ApiCostEntry
{
    public long Id { get; set; }
    public string SimulationRunId { get; set; } = "";
    public string TaskType { get; set; } = "";
    public string Model { get; set; } = "";
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal Cost { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
