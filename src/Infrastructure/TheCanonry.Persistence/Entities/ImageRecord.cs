namespace TheCanonry.Persistence.Entities;

public class ImageRecord
{
    public long Id { get; set; }
    public string SimulationRunId { get; set; } = "";
    public string? EntityId { get; set; }
    public string Prompt { get; set; } = "";
    public string Model { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public string Aspect { get; set; } = "square";
    public string Type { get; set; } = "entity";
    public string FilePath { get; set; } = "";
    public string? HqFilePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
