namespace TheCanonry.Persistence.Entities;

public class SummaryRevisionRunEntity
{
    public long Id { get; set; }
    public string RunId { get; set; } = "";
    public string SimulationRunId { get; set; } = "";
    public string BatchesJson { get; set; } = "[]"; // JSON: batch structure with per-entity patches
    public string Status { get; set; } = "pending";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
