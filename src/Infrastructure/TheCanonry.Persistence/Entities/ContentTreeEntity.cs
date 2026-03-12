namespace TheCanonry.Persistence.Entities;

public class ContentTreeEntity
{
    public long Id { get; set; }
    public string ProjectId { get; set; } = "";
    public string SimulationRunId { get; set; } = "";
    public string TreeJson { get; set; } = "[]"; // JSON: ContentTreeNode[]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
