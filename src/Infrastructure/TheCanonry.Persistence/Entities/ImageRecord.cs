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

    // Catalog metadata (populated by LLM classification or manual assignment)
    public string? Title { get; set; }
    public string? Tags { get; set; } // JSON array of strings
    public string? ArtisticStyleId { get; set; }
    public string? CompositionStyleId { get; set; }
    public string? ColorPaletteId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
