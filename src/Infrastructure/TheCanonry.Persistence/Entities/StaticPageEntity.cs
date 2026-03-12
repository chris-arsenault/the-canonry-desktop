namespace TheCanonry.Persistence.Entities;

public class StaticPageEntity
{
    public long Id { get; set; }
    public string PageId { get; set; } = ""; // Original string ID from IndexedDB
    public string ProjectId { get; set; } = "";
    public string SimulationRunId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Content { get; set; } = ""; // Markdown body
    public string? Summary { get; set; }
    public string Status { get; set; } = "draft"; // draft | published
    public string LinkedEntityIdsJson { get; set; } = "[]"; // JSON: string[]
    public int WordCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
