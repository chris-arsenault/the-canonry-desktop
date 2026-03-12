namespace TheCanonry.AwsSync.Sync;

using System.Text.Json;
using TheCanonry.AwsSync.S3;
using TheCanonry.AwsSync.Types;

public sealed class ManifestManager(IS3Operations s3, string basePrefix)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _manifestKey = string.IsNullOrEmpty(basePrefix)
        ? "image-manifest.json"
        : $"{basePrefix}/image-manifest.json";

    public async Task<ImageManifest> LoadAsync(CancellationToken ct)
    {
        var data = await s3.GetObjectAsync(_manifestKey, ct);
        if (data is null)
            return new ImageManifest { UpdatedAt = DateTime.UtcNow };

        return JsonSerializer.Deserialize<ImageManifest>(data, JsonOptions)
            ?? new ImageManifest { UpdatedAt = DateTime.UtcNow };
    }

    public async Task SaveAsync(ImageManifest manifest, CancellationToken ct)
    {
        manifest.UpdatedAt = DateTime.UtcNow;
        var json = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions);
        await s3.PutObjectAsync(
            _manifestKey,
            json,
            "application/json",
            cacheControl: "no-store, must-revalidate",
            ct: ct);
    }

    public string BuildRawKey(string projectId, string imageId) =>
        string.IsNullOrEmpty(basePrefix)
            ? $"raw/{projectId}/{imageId}"
            : $"{basePrefix}/raw/{projectId}/{imageId}";

    public string BuildWebpKey(string projectId, string imageId) =>
        string.IsNullOrEmpty(basePrefix)
            ? $"webp/{projectId}/{imageId}.webp"
            : $"{basePrefix}/webp/{projectId}/{imageId}.webp";

    public string BuildThumbKey(string projectId, string imageId) =>
        string.IsNullOrEmpty(basePrefix)
            ? $"thumb/{projectId}/{imageId}.webp"
            : $"{basePrefix}/thumb/{projectId}/{imageId}.webp";

    public string BuildHqKey(string projectId, string imageId) =>
        string.IsNullOrEmpty(basePrefix)
            ? $"hq/{projectId}/{imageId}.png"
            : $"{basePrefix}/hq/{projectId}/{imageId}.png";
}
