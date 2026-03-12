namespace TheCanonry.Persistence.Entities;

public class PageLayoutEntity
{
    public long Id { get; set; }
    public string SimulationRunId { get; set; } = "";
    public string PageId { get; set; } = ""; // References StaticPage, Chronicle, etc.
    public string LayoutMode { get; set; } = "default";
    public string AnnotationDisplay { get; set; } = "inline";
    public string ImageLayout { get; set; } = "float";
    public string ContentWidth { get; set; } = "normal";
    public string SettingsJson { get; set; } = "{}"; // JSON: additional overrides
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
