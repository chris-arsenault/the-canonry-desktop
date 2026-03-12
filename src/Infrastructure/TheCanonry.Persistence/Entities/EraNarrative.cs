namespace TheCanonry.Persistence.Entities;

public class EraNarrative
{
    public long Id { get; set; }
    public string SimulationRunId { get; set; } = "";
    public string EraId { get; set; } = "";
    public string? Threads { get; set; } // JSON
    public string Content { get; set; } = "";
    public string? Summary { get; set; }
    public string? CoverScene { get; set; }
    public string? ImageRefs { get; set; } // JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
