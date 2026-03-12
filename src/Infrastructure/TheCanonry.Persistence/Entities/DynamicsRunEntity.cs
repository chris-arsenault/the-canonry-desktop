namespace TheCanonry.Persistence.Entities;

public class DynamicsRunEntity
{
    public long Id { get; set; }
    public string RunId { get; set; } = "";
    public string SimulationRunId { get; set; } = "";
    public string MessagesJson { get; set; } = "[]"; // JSON: conversation message history
    public string Status { get; set; } = "pending"; // pending, running, completed, failed
    public string? ResultJson { get; set; } // JSON: generated dynamics content
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
