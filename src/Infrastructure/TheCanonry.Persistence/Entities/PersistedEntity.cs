namespace TheCanonry.Persistence.Entities;

public class PersistedEntity
{
    public required string Id { get; set; }
    public required string SimulationRunId { get; set; }
    public required string Kind { get; set; }
    public required string Subtype { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Summary { get; set; }
    public required string Status { get; set; }
    public required double Prominence { get; set; }
    public required string Culture { get; set; }
    public required string EraId { get; set; }
    public required double CoordX { get; set; }
    public required double CoordY { get; set; }
    public required double CoordZ { get; set; }
    public required int CreatedAtTick { get; set; }
    public required int UpdatedAtTick { get; set; }

    /// <summary>JSON-serialized EntityTags</summary>
    public string TagsJson { get; set; } = "{}";

    /// <summary>JSON-serialized enrichment data (from Illuminator)</summary>
    public string? EnrichmentJson { get; set; }
}
