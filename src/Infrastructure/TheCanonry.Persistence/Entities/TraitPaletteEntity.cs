namespace TheCanonry.Persistence.Entities;

public class TraitPaletteEntity
{
    public long Id { get; set; }
    public string ProjectId { get; set; } = "";
    public string EntityKind { get; set; } = "";
    public string CategoriesJson { get; set; } = "[]"; // JSON: TraitCategory[]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
