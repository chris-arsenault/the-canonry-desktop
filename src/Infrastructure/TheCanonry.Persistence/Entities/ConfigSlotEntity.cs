namespace TheCanonry.Persistence.Entities;

/// <summary>
/// General-purpose config storage slot. Each (ProjectId, ConfigType) pair holds one JSON blob.
/// </summary>
public class ConfigSlotEntity
{
    public long Id { get; set; }
    public string ProjectId { get; set; } = "";
    public string ConfigType { get; set; } = "";
    public string ValueJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
