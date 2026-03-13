using SkiaSharp;
using TheCanonry.AwsSync.Sync;
using TheCanonry.AwsSync.Types;

namespace TheCanonry.AwsSync.Tests;

public sealed class ImageSyncServiceTests : IDisposable
{
    private readonly InMemoryS3 _s3 = new();
    private readonly string _tempDir;
    private readonly AwsSyncConfig _config;

    public ImageSyncServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"aws-sync-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _config = new AwsSyncConfig(
            Region: "us-east-1",
            BucketName: "test-bucket",
            BasePrefix: "test-images",
            ProfileName: null,
            AccessKeyId: null,
            SecretAccessKey: null,
            CdnBaseUrl: null);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static byte[] CreateTestPng(int width = 64, int height = 64)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Blue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private string WriteTestImage(string filename, int width = 64, int height = 64)
    {
        var path = Path.Combine(_tempDir, filename);
        File.WriteAllBytes(path, CreateTestPng(width, height));
        return path;
    }

    private string WriteTestImageInSubDir(string subDir, string filename, int width = 64, int height = 64)
    {
        var dir = Path.Combine(_tempDir, subDir);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, filename);
        File.WriteAllBytes(path, CreateTestPng(width, height));
        return path;
    }

    // ── TestConnectionAsync ─────────────────────────────────────────────

    [Fact]
    public async Task TestConnectionAsync_returns_true_when_s3_accessible()
    {
        var service = new ImageSyncService(_s3, _config);

        var result = await service.TestConnectionAsync(CancellationToken.None);

        Assert.True(result);
    }

    // ── SyncAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SyncAsync_uploads_new_images_to_s3()
    {
        WriteTestImageInSubDir("proj-1", "img-001.png");
        WriteTestImageInSubDir("proj-1", "img-002.png");

        var service = new ImageSyncService(_s3, _config);
        await service.SyncAsync(_tempDir, "proj-1", ct: CancellationToken.None);

        // Raw images should be in S3
        Assert.True(_s3.Contains("test-images/raw/proj-1/img-001"));
        Assert.True(_s3.Contains("test-images/raw/proj-1/img-002"));
    }

    [Fact]
    public async Task SyncAsync_creates_webp_and_thumbnail_variants()
    {
        WriteTestImageInSubDir("proj-1", "img-001.png", width: 800, height: 600);

        var service = new ImageSyncService(_s3, _config);
        await service.SyncAsync(_tempDir, "proj-1", ct: CancellationToken.None);

        Assert.True(_s3.Contains("test-images/webp/proj-1/img-001.webp"));
        Assert.True(_s3.Contains("test-images/thumb/proj-1/img-001.webp"));
    }

    [Fact]
    public async Task SyncAsync_updates_manifest_with_new_entries()
    {
        WriteTestImageInSubDir("proj-1", "img-abc.png", width: 512, height: 384);

        var service = new ImageSyncService(_s3, _config);
        await service.SyncAsync(_tempDir, "proj-1", ct: CancellationToken.None);

        // Manifest should exist and contain the entry
        Assert.True(_s3.Contains("test-images/image-manifest.json"));

        var manager = new ManifestManager(_s3, "test-images");
        var manifest = await manager.LoadAsync(CancellationToken.None);

        Assert.True(manifest.Entries.ContainsKey("img-abc"));
        var entry = manifest.Entries["img-abc"];
        Assert.Equal("proj-1", entry.ProjectId);
        Assert.Equal(512, entry.Width);
        Assert.Equal(384, entry.Height);
    }

    [Fact]
    public async Task SyncAsync_skips_already_synced_images()
    {
        WriteTestImageInSubDir("proj-1", "img-001.png");

        var service = new ImageSyncService(_s3, _config);

        // First sync
        await service.SyncAsync(_tempDir, "proj-1", ct: CancellationToken.None);
        var countAfterFirst = _s3.Count;

        // Second sync of the same directory should not increase object count
        await service.SyncAsync(_tempDir, "proj-1", ct: CancellationToken.None);

        Assert.Equal(countAfterFirst, _s3.Count);
    }

    [Fact]
    public async Task SyncAsync_handles_hq_variants()
    {
        // Simulate an HQ variant alongside the regular image
        WriteTestImageInSubDir("proj-1", "img-001.png", width: 512, height: 512);
        // HQ file: {projectDir}/{imageId}.hq.png
        var hqDir = Path.Combine(_tempDir, "proj-1");
        await File.WriteAllBytesAsync(Path.Combine(hqDir, "img-001.hq.png"), CreateTestPng(2048, 2048));

        var service = new ImageSyncService(_s3, _config);
        await service.SyncAsync(_tempDir, "proj-1", ct: CancellationToken.None);

        // HQ variant should be uploaded
        Assert.True(_s3.Contains("test-images/hq/proj-1/img-001.png"));
    }

    [Fact]
    public async Task SyncAsync_reports_progress()
    {
        WriteTestImageInSubDir("proj-1", "img-001.png");

        var progressReports = new List<SyncProgress>();
        var progress = new Progress<SyncProgress>(p => progressReports.Add(p));

        var service = new ImageSyncService(_s3, _config);
        await service.SyncAsync(_tempDir, "proj-1", progress: progress, ct: CancellationToken.None);

        Assert.NotEmpty(progressReports);
    }

    [Fact]
    public async Task SyncAsync_respects_cancellation()
    {
        WriteTestImageInSubDir("proj-1", "img-001.png");

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Cancel immediately

        var service = new ImageSyncService(_s3, _config);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.SyncAsync(_tempDir, "proj-1", ct: cts.Token));
    }

    // ── PullAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task PullAsync_downloads_images_from_s3()
    {
        // Seed S3 with a manifest and a raw image (imageId has no extension)
        var png = CreateTestPng(256, 256);
        await _s3.PutObjectAsync("test-images/raw/proj-1/img-001", png, "image/png",
            ct: CancellationToken.None);

        var manifest = new ImageManifest
        {
            Entries = new Dictionary<string, ManifestEntry>
            {
                ["img-001"] = new()
                {
                    ImageId = "img-001",
                    ProjectId = "proj-1",
                    RawKey = "test-images/raw/proj-1/img-001",
                    MimeType = "image/png",
                    Width = 256,
                    Height = 256
                }
            }
        };
        var manager = new ManifestManager(_s3, "test-images");
        await manager.SaveAsync(manifest, CancellationToken.None);

        var pullDir = Path.Combine(_tempDir, "pulled");
        Directory.CreateDirectory(pullDir);

        var service = new ImageSyncService(_s3, _config);
        await service.PullAsync(pullDir, projectId: "proj-1", ct: CancellationToken.None);

        // Pull saves to {imageDir}/{projectId}/{imageId}.png
        Assert.True(File.Exists(Path.Combine(pullDir, "proj-1", "img-001.png")));
    }

    [Fact]
    public async Task PullAsync_skips_existing_local_images()
    {
        // imageId has no extension (matches SyncAsync convention)
        var png = CreateTestPng(128, 128);
        await _s3.PutObjectAsync("test-images/raw/proj-1/img-exist", png, "image/png",
            ct: CancellationToken.None);

        var manifest = new ImageManifest
        {
            Entries = new Dictionary<string, ManifestEntry>
            {
                ["img-exist"] = new()
                {
                    ImageId = "img-exist",
                    ProjectId = "proj-1",
                    RawKey = "test-images/raw/proj-1/img-exist",
                    MimeType = "image/png",
                    Width = 128,
                    Height = 128
                }
            }
        };
        var manager = new ManifestManager(_s3, "test-images");
        await manager.SaveAsync(manifest, CancellationToken.None);

        var pullDir = Path.Combine(_tempDir, "pulled");
        // Create the file at the path Pull will check: {imageDir}/{projectId}/{imageId}.png
        var projDir = Path.Combine(pullDir, "proj-1");
        Directory.CreateDirectory(projDir);
        var localPath = Path.Combine(projDir, "img-exist.png");
        var localContent = new byte[] { 0xDE, 0xAD }; // Sentinel value
        await File.WriteAllBytesAsync(localPath, localContent);

        var service = new ImageSyncService(_s3, _config);
        await service.PullAsync(pullDir, projectId: "proj-1", ct: CancellationToken.None);

        // Local file should not be overwritten
        var afterPull = await File.ReadAllBytesAsync(localPath);
        Assert.Equal(localContent, afterPull);
    }
}
