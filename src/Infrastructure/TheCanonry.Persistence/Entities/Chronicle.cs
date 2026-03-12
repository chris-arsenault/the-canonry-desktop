namespace TheCanonry.Persistence.Entities;

public class Chronicle
{
    public long Id { get; set; }
    public string SimulationRunId { get; set; } = "";
    public string EntityIds { get; set; } = ""; // JSON array
    public string Format { get; set; } = "narrative";
    public string Content { get; set; } = "";
    public string? Summary { get; set; }
    public string? Title { get; set; }
    public string? ImageRefs { get; set; } // JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
}
