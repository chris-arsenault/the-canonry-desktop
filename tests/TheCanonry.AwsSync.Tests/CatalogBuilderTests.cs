namespace TheCanonry.AwsSync.Tests;

using System.Text.Json;
using TheCanonry.AwsSync.Sync;
using TheCanonry.AwsSync.Types;

public class CatalogBuilderTests
{
    private readonly InMemoryS3 _s3 = new();

    // ── BuildAndUploadAsync ─────────────────────────────────────────────

    [Fact]
    public async Task BuildAndUploadAsync_creates_catalog_json_in_s3()
    {
        var manifest = new ImageManifest
        {
            Version = 1,
            UpdatedAt = DateTime.UtcNow,
            Entries = new Dictionary<string, ManifestEntry>
            {
                ["img-001"] = new()
                {
                    ImageId = "img-001",
                    ProjectId = "proj-1",
                    EntityId = "ent-1",
                    RawKey = "prefix/raw/proj-1/img-001",
                    WebpKey = "prefix/webp/proj-1/img-001.webp",
                    ThumbKey = "prefix/thumb/proj-1/img-001.webp",
                    HqKey = "",
                    MimeType = "image/png",
                    Width = 1024,
                    Height = 768,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        var builder = new CatalogBuilder(_s3, "prefix");
        await builder.BuildAndUploadAsync(manifest, CancellationToken.None);

        Assert.True(_s3.Contains("prefix/catalog.json"));
    }

    [Fact]
    public async Task Catalog_entries_match_manifest_entries()
    {
        var manifest = new ImageManifest
        {
            Version = 1,
            UpdatedAt = DateTime.UtcNow,
            Entries = new Dictionary<string, ManifestEntry>
            {
                ["img-alpha"] = new()
                {
                    ImageId = "img-alpha",
                    ProjectId = "proj-1",
                    EntityId = "ent-a",
                    RawKey = "cdn/raw/proj-1/img-alpha",
                    WebpKey = "cdn/webp/proj-1/img-alpha.webp",
                    ThumbKey = "cdn/thumb/proj-1/img-alpha.webp",
                    HqKey = "cdn/hq/proj-1/img-alpha.png",
                    MimeType = "image/png",
                    Width = 800,
                    Height = 600,
                    HqWidth = 3200,
                    HqHeight = 2400,
                    UpdatedAt = DateTime.UtcNow
                },
                ["img-beta"] = new()
                {
                    ImageId = "img-beta",
                    ProjectId = "proj-1",
                    EntityId = "ent-b",
                    RawKey = "cdn/raw/proj-1/img-beta",
                    WebpKey = "cdn/webp/proj-1/img-beta.webp",
                    ThumbKey = "cdn/thumb/proj-1/img-beta.webp",
                    HqKey = "",
                    MimeType = "image/png",
                    Width = 512,
                    Height = 512,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        var builder = new CatalogBuilder(_s3, "cdn");
        await builder.BuildAndUploadAsync(manifest, CancellationToken.None);

        var data = _s3.GetData("cdn/catalog.json");
        Assert.NotNull(data);

        var json = JsonSerializer.Deserialize<JsonElement>(data);
        Assert.Equal(JsonValueKind.Object, json.ValueKind);
        var entries = json.GetProperty("entries");
        Assert.Equal(JsonValueKind.Array, entries.ValueKind);
        Assert.Equal(2, entries.GetArrayLength());
    }

    [Fact]
    public async Task Catalog_includes_correct_paths()
    {
        var manifest = new ImageManifest
        {
            Version = 1,
            UpdatedAt = DateTime.UtcNow,
            Entries = new Dictionary<string, ManifestEntry>
            {
                ["img-001"] = new()
                {
                    ImageId = "img-001",
                    ProjectId = "proj-1",
                    EntityId = "ent-1",
                    RawKey = "assets/raw/proj-1/img-001",
                    WebpKey = "assets/webp/proj-1/img-001.webp",
                    ThumbKey = "assets/thumb/proj-1/img-001.webp",
                    HqKey = "assets/hq/proj-1/img-001.png",
                    MimeType = "image/png",
                    Width = 640,
                    Height = 480,
                    HqWidth = 2560,
                    HqHeight = 1920,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        var builder = new CatalogBuilder(_s3, "assets");
        await builder.BuildAndUploadAsync(manifest, CancellationToken.None);

        var data = _s3.GetData("assets/catalog.json");
        Assert.NotNull(data);

        var text = System.Text.Encoding.UTF8.GetString(data);

        // The catalog should reference the correct S3 keys
        Assert.Contains("img-001", text);
        Assert.Contains("proj-1", text);
    }

    [Fact]
    public async Task BuildAndUploadAsync_handles_empty_manifest()
    {
        var manifest = new ImageManifest
        {
            Version = 1,
            UpdatedAt = DateTime.UtcNow,
            Entries = []
        };

        var builder = new CatalogBuilder(_s3, "empty");
        await builder.BuildAndUploadAsync(manifest, CancellationToken.None);

        Assert.True(_s3.Contains("empty/catalog.json"));

        var data = _s3.GetData("empty/catalog.json");
        Assert.NotNull(data);

        var json = JsonSerializer.Deserialize<JsonElement>(data);
        Assert.Equal(JsonValueKind.Object, json.ValueKind);
        var entries = json.GetProperty("entries");
        Assert.Equal(JsonValueKind.Array, entries.ValueKind);
        Assert.Equal(0, entries.GetArrayLength());
    }

    [Fact]
    public async Task BuildAndUploadAsync_with_empty_prefix()
    {
        var manifest = new ImageManifest
        {
            Version = 1,
            UpdatedAt = DateTime.UtcNow,
            Entries = new Dictionary<string, ManifestEntry>
            {
                ["img-x"] = new()
                {
                    ImageId = "img-x",
                    ProjectId = "p1",
                    RawKey = "raw/p1/img-x",
                    MimeType = "image/png",
                    Width = 100,
                    Height = 100,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        var builder = new CatalogBuilder(_s3, "");
        await builder.BuildAndUploadAsync(manifest, CancellationToken.None);

        Assert.True(_s3.Contains("catalog.json"));
    }
}
