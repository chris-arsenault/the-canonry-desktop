namespace TheCanonry.AwsSync.Types;

using System.Text.Json.Serialization;

public sealed class ImageManifest
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("entries")]
    public Dictionary<string, ManifestEntry> Entries { get; set; } = [];
}

public sealed class ManifestEntry
{
    [JsonPropertyName("imageId")]
    public required string ImageId { get; init; }

    [JsonPropertyName("projectId")]
    public required string ProjectId { get; init; }

    [JsonPropertyName("entityId")]
    public string EntityId { get; set; } = "";

    [JsonPropertyName("rawKey")]
    public string RawKey { get; set; } = "";

    [JsonPropertyName("webpKey")]
    public string WebpKey { get; set; } = "";

    [JsonPropertyName("thumbKey")]
    public string ThumbKey { get; set; } = "";

    [JsonPropertyName("hqKey")]
    public string HqKey { get; set; } = "";

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "image/png";

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("hqWidth")]
    public int HqWidth { get; set; }

    [JsonPropertyName("hqHeight")]
    public int HqHeight { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
