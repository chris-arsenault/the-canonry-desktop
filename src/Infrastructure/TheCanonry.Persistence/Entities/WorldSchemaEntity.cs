namespace TheCanonry.Persistence.Entities;

public class WorldSchemaEntity
{
    public long Id { get; set; }
    public string ProjectId { get; set; } = "";
    public string SchemaJson { get; set; } = "{}"; // Full CanonrySchemaSlice
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
