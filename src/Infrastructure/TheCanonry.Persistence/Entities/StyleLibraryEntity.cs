namespace TheCanonry.Persistence.Entities;

public class StyleLibraryEntity
{
    public long Id { get; set; }
    public string ProjectId { get; set; } = "";
    public string ArtisticStylesJson { get; set; } = "[]";
    public string CompositionStylesJson { get; set; } = "[]";
    public string ColorPalettesJson { get; set; } = "[]";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
