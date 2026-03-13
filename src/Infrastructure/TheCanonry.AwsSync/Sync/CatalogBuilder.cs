using System.Text.Json;
using System.Text.Json.Serialization;
using TheCanonry.AwsSync.S3;
using TheCanonry.AwsSync.Types;

namespace TheCanonry.AwsSync.Sync;

public sealed class CatalogBuilder(IS3Operations s3, string basePrefix)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task BuildAndUploadAsync(
        ImageManifest manifest,
        CancellationToken ct)
    {
        var entries = manifest.Entries.Values
            .Select(e => new CatalogEntry
            {
                Id = e.ImageId,
                EntityId = e.EntityId,
                RawPath = e.RawKey,
                WebpPath = e.WebpKey,
                ThumbPath = e.ThumbKey,
                HqPath = e.HqKey,
                Width = e.Width,
                Height = e.Height,
                HqWidth = e.HqWidth,
                HqHeight = e.HqHeight
            })
            .OrderBy(e => e.Id, StringComparer.Ordinal)
            .ToList();

        var catalog = new Catalog
        {
            Version = 1,
            GeneratedAt = DateTime.UtcNow,
            Entries = entries
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(catalog, JsonOptions);

        var catalogKey = string.IsNullOrEmpty(basePrefix)
            ? "catalog.json"
            : $"{basePrefix}/catalog.json";

        await s3.PutObjectAsync(
            catalogKey,
            json,
            "application/json",
            cacheControl: "no-store, must-revalidate",
            ct: ct);
    }
}

public sealed class CatalogEntry
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("entityId")]
    public string EntityId { get; init; } = "";

    [JsonPropertyName("rawPath")]
    public required string RawPath { get; init; }

    [JsonPropertyName("webpPath")]
    public string WebpPath { get; init; } = "";

    [JsonPropertyName("thumbPath")]
    public string ThumbPath { get; init; } = "";

    [JsonPropertyName("hqPath")]
    public string HqPath { get; init; } = "";

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("hqWidth")]
    public int HqWidth { get; init; }

    [JsonPropertyName("hqHeight")]
    public int HqHeight { get; init; }
}

public sealed class Catalog
{
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; init; }

    [JsonPropertyName("entries")]
    public required IReadOnlyList<CatalogEntry> Entries { get; init; }
}
