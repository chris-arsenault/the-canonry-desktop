using System.Text.Json;
using TheCanonry.AwsSync.Sync;
using TheCanonry.AwsSync.Types;

namespace TheCanonry.AwsSync.Tests;

public class ManifestManagerTests
{
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };
    private readonly InMemoryS3 _s3 = new();

    // ── LoadAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_returns_empty_manifest_when_none_exists()
    {
        var manager = new ManifestManager(_s3, "images");

        var manifest = await manager.LoadAsync(CancellationToken.None);

        Assert.NotNull(manifest);
        Assert.Empty(manifest.Entries);
        Assert.Equal(1, manifest.Version);
    }

    [Fact]
    public async Task LoadAsync_deserializes_existing_manifest()
    {
        var existing = new ImageManifest
        {
            Version = 2,
            UpdatedAt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            Entries = new Dictionary<string, ManifestEntry>
            {
                ["img-001"] = new()
                {
                    ImageId = "img-001",
                    ProjectId = "proj-1",
                    EntityId = "ent-1",
                    RawKey = "images/raw/proj-1/img-001",
                    WebpKey = "images/webp/proj-1/img-001.webp",
                    ThumbKey = "images/thumb/proj-1/img-001.webp",
                    HqKey = "images/hq/proj-1/img-001.png",
                    MimeType = "image/png",
                    Width = 1024,
                    Height = 768,
                    UpdatedAt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)
                }
            }
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(existing, IndentedOptions);
        await _s3.PutObjectAsync("images/image-manifest.json", json, "application/json", ct: CancellationToken.None);

        var manager = new ManifestManager(_s3, "images");
        var loaded = await manager.LoadAsync(CancellationToken.None);

        Assert.Equal(2, loaded.Version);
        Assert.Single(loaded.Entries);
        Assert.True(loaded.Entries.ContainsKey("img-001"));
        Assert.Equal("ent-1", loaded.Entries["img-001"].EntityId);
        Assert.Equal(1024, loaded.Entries["img-001"].Width);
        Assert.Equal(768, loaded.Entries["img-001"].Height);
    }

    // ── SaveAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_serializes_and_uploads_manifest()
    {
        var manager = new ManifestManager(_s3, "myprefix");
        var manifest = new ImageManifest
        {
            Version = 1,
            Entries = new Dictionary<string, ManifestEntry>
            {
                ["img-abc"] = new()
                {
                    ImageId = "img-abc",
                    ProjectId = "proj-x",
                    RawKey = "myprefix/raw/proj-x/img-abc",
                    MimeType = "image/png",
                    Width = 512,
                    Height = 512
                }
            }
        };

        await manager.SaveAsync(manifest, CancellationToken.None);

        Assert.True(_s3.Contains("myprefix/image-manifest.json"));

        var data = _s3.GetData("myprefix/image-manifest.json");
        Assert.NotNull(data);

        var roundTrip = JsonSerializer.Deserialize<ImageManifest>(data);
        Assert.NotNull(roundTrip);
        Assert.Single(roundTrip.Entries);
        Assert.Equal("img-abc", roundTrip.Entries["img-abc"].ImageId);
    }

    [Fact]
    public async Task SaveAsync_updates_timestamp()
    {
        var manager = new ManifestManager(_s3, "pfx");
        var manifest = new ImageManifest
        {
            UpdatedAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var before = DateTime.UtcNow;
        await manager.SaveAsync(manifest, CancellationToken.None);
        var after = DateTime.UtcNow;

        // The manifest's UpdatedAt should have been refreshed to ~now
        Assert.True(manifest.UpdatedAt >= before);
        Assert.True(manifest.UpdatedAt <= after);
    }

    // ── Key building ────────────────────────────────────────────────────

    [Theory]
    [InlineData("images", "proj-1", "img-001", "images/raw/proj-1/img-001")]
    [InlineData("cdn/v2", "proj-a", "img-xyz", "cdn/v2/raw/proj-a/img-xyz")]
    public void BuildRawKey_returns_correct_path(string prefix, string projectId, string imageId, string expected)
    {
        var manager = new ManifestManager(_s3, prefix);
        Assert.Equal(expected, manager.BuildRawKey(projectId, imageId));
    }

    [Theory]
    [InlineData("images", "proj-1", "img-001", "images/webp/proj-1/img-001.webp")]
    [InlineData("cdn/v2", "proj-a", "img-xyz", "cdn/v2/webp/proj-a/img-xyz.webp")]
    public void BuildWebpKey_returns_correct_path(string prefix, string projectId, string imageId, string expected)
    {
        var manager = new ManifestManager(_s3, prefix);
        Assert.Equal(expected, manager.BuildWebpKey(projectId, imageId));
    }

    [Theory]
    [InlineData("images", "proj-1", "img-001", "images/thumb/proj-1/img-001.webp")]
    public void BuildThumbKey_returns_correct_path(string prefix, string projectId, string imageId, string expected)
    {
        var manager = new ManifestManager(_s3, prefix);
        Assert.Equal(expected, manager.BuildThumbKey(projectId, imageId));
    }

    [Theory]
    [InlineData("images", "proj-1", "img-001", "images/hq/proj-1/img-001.png")]
    public void BuildHqKey_returns_correct_path(string prefix, string projectId, string imageId, string expected)
    {
        var manager = new ManifestManager(_s3, prefix);
        Assert.Equal(expected, manager.BuildHqKey(projectId, imageId));
    }

    // ── Empty prefix ────────────────────────────────────────────────────

    [Fact]
    public void Key_paths_have_no_leading_slash_when_prefix_is_empty()
    {
        var manager = new ManifestManager(_s3, "");

        Assert.Equal("raw/proj-1/img-001", manager.BuildRawKey("proj-1", "img-001"));
        Assert.Equal("webp/proj-1/img-001.webp", manager.BuildWebpKey("proj-1", "img-001"));
        Assert.Equal("thumb/proj-1/img-001.webp", manager.BuildThumbKey("proj-1", "img-001"));
        Assert.Equal("hq/proj-1/img-001.png", manager.BuildHqKey("proj-1", "img-001"));
    }

    [Fact]
    public async Task LoadAsync_uses_root_manifest_key_when_prefix_is_empty()
    {
        var manifest = new ImageManifest { Version = 3 };
        var json = JsonSerializer.SerializeToUtf8Bytes(manifest);
        await _s3.PutObjectAsync("image-manifest.json", json, "application/json", ct: CancellationToken.None);

        var manager = new ManifestManager(_s3, "");
        var loaded = await manager.LoadAsync(CancellationToken.None);

        Assert.Equal(3, loaded.Version);
    }
}
