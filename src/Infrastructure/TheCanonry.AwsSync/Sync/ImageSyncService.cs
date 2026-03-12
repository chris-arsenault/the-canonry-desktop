namespace TheCanonry.AwsSync.Sync;

using TheCanonry.AwsSync.S3;
using TheCanonry.AwsSync.Types;

public sealed class ImageSyncService
{
    private readonly IS3Operations _s3;
    private readonly ManifestManager _manifest;
    private readonly AwsSyncConfig _config;

    private const string ImageCacheControl = "public, max-age=31536000, immutable";

    public ImageSyncService(IS3Operations s3, AwsSyncConfig config)
    {
        _s3 = s3;
        _config = config;
        _manifest = new ManifestManager(s3, config.BasePrefix);
    }

    /// <summary>
    /// Sync images from local storage to S3.
    /// </summary>
    public async Task<ImageManifest> SyncAsync(
        string imageDir,
        string projectId,
        IProgress<SyncProgress>? progress = null,
        CancellationToken ct = default)
    {
        // 1. Load manifest
        progress?.Report(new SyncProgress(SyncPhase.Scanning, 0, 0, "Loading manifest..."));
        var manifest = await _manifest.LoadAsync(ct);

        // 2. Scan local image directory
        var projectDir = Path.Combine(imageDir, projectId);
        if (!Directory.Exists(projectDir))
            return manifest;

        var rawFiles = Directory.GetFiles(projectDir, "*.png")
            .Where(f => !f.EndsWith(".hq.png", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var hqFiles = Directory.GetFiles(projectDir, "*.hq.png")
            .ToDictionary(
                f => Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(f)),
                f => f,
                StringComparer.OrdinalIgnoreCase);

        var totalItems = rawFiles.Count;
        progress?.Report(new SyncProgress(SyncPhase.Scanning, totalItems, 0, $"Found {totalItems} images"));

        // 3. Process each image
        var completed = 0;
        foreach (var rawFile in rawFiles)
        {
            ct.ThrowIfCancellationRequested();

            var imageId = Path.GetFileNameWithoutExtension(rawFile);
            var rawKey = _manifest.BuildRawKey(projectId, imageId);
            var webpKey = _manifest.BuildWebpKey(projectId, imageId);
            var thumbKey = _manifest.BuildThumbKey(projectId, imageId);
            var hqKey = _manifest.BuildHqKey(projectId, imageId);

            var localModified = File.GetLastWriteTimeUtc(rawFile);
            var needsSync = !manifest.Entries.TryGetValue(imageId, out var existing)
                || existing.UpdatedAt < localModified;

            if (needsSync)
            {
                var rawData = await File.ReadAllBytesAsync(rawFile, ct);
                var (width, height) = ImageVariantGenerator.GetDimensions(rawData);

                // Upload raw
                progress?.Report(new SyncProgress(SyncPhase.Uploading, totalItems, completed, imageId));
                var tags = new Dictionary<string, string>
                {
                    ["projectId"] = projectId,
                    ["imageId"] = imageId
                };
                await _s3.PutObjectAsync(rawKey, rawData, "image/png", tags, ImageCacheControl, ct);

                // Generate and upload WebP
                progress?.Report(new SyncProgress(SyncPhase.GeneratingVariants, totalItems, completed, imageId));
                var webpData = ImageVariantGenerator.ConvertToWebp(rawData);

                progress?.Report(new SyncProgress(SyncPhase.UploadingVariants, totalItems, completed, imageId));
                await _s3.PutObjectAsync(webpKey, webpData, "image/webp", tags, ImageCacheControl, ct);

                // Generate and upload thumbnail
                var thumbData = ImageVariantGenerator.GenerateThumbnail(rawData);
                await _s3.PutObjectAsync(thumbKey, thumbData, "image/webp", tags, ImageCacheControl, ct);

                // Update manifest entry
                var entry = new ManifestEntry
                {
                    ImageId = imageId,
                    ProjectId = projectId,
                    RawKey = rawKey,
                    WebpKey = webpKey,
                    ThumbKey = thumbKey,
                    MimeType = "image/png",
                    Width = width,
                    Height = height,
                    UpdatedAt = DateTime.UtcNow
                };

                // Handle HQ variant
                if (hqFiles.TryGetValue(imageId, out var hqFile))
                {
                    progress?.Report(new SyncProgress(SyncPhase.UploadingHq, totalItems, completed, imageId));
                    var hqData = await File.ReadAllBytesAsync(hqFile, ct);
                    var (hqWidth, hqHeight) = ImageVariantGenerator.GetDimensions(hqData);
                    await _s3.PutObjectAsync(hqKey, hqData, "image/png", tags, ImageCacheControl, ct);

                    entry.HqKey = hqKey;
                    entry.HqWidth = hqWidth;
                    entry.HqHeight = hqHeight;
                }

                manifest.Entries[imageId] = entry;
            }
            else if (existing is not null && hqFiles.TryGetValue(imageId, out var hqFile2)
                && string.IsNullOrEmpty(existing.HqKey))
            {
                // Existing entry but HQ variant is new
                progress?.Report(new SyncProgress(SyncPhase.UploadingHq, totalItems, completed, imageId));
                var hqData = await File.ReadAllBytesAsync(hqFile2, ct);
                var (hqWidth, hqHeight) = ImageVariantGenerator.GetDimensions(hqData);
                var tags = new Dictionary<string, string>
                {
                    ["projectId"] = projectId,
                    ["imageId"] = imageId
                };
                await _s3.PutObjectAsync(hqKey, hqData, "image/png", tags, ImageCacheControl, ct);

                existing.HqKey = hqKey;
                existing.HqWidth = hqWidth;
                existing.HqHeight = hqHeight;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            completed++;
        }

        // 4. Save manifest
        progress?.Report(new SyncProgress(SyncPhase.UpdatingManifest, totalItems, completed, "Saving manifest..."));
        await _manifest.SaveAsync(manifest, ct);

        // 5. Build catalog
        progress?.Report(new SyncProgress(SyncPhase.BuildingCatalog, totalItems, completed, "Building catalog..."));
        var catalogBuilder = new CatalogBuilder(_s3, _config.BasePrefix);
        await catalogBuilder.BuildAndUploadAsync(manifest, ct);

        progress?.Report(new SyncProgress(SyncPhase.Complete, totalItems, totalItems, "Sync complete"));
        return manifest;
    }

    /// <summary>
    /// Pull images from S3 to local storage.
    /// </summary>
    public async Task PullAsync(
        string imageDir,
        string? projectId = null,
        IProgress<SyncProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new SyncProgress(SyncPhase.Scanning, 0, 0, "Loading manifest..."));
        var manifest = await _manifest.LoadAsync(ct);

        var entries = manifest.Entries.Values.AsEnumerable();
        if (projectId is not null)
            entries = entries.Where(e => e.ProjectId == projectId);

        var entryList = entries.ToList();
        var totalItems = entryList.Count;
        var completed = 0;

        progress?.Report(new SyncProgress(SyncPhase.Scanning, totalItems, 0, $"Found {totalItems} entries"));

        foreach (var entry in entryList)
        {
            ct.ThrowIfCancellationRequested();

            var localPath = Path.Combine(imageDir, entry.ProjectId, $"{entry.ImageId}.png");
            var localDir = Path.GetDirectoryName(localPath)!;

            if (!File.Exists(localPath))
            {
                progress?.Report(new SyncProgress(SyncPhase.Uploading, totalItems, completed, entry.ImageId));

                var data = await _s3.GetObjectAsync(entry.RawKey, ct);
                if (data is not null)
                {
                    Directory.CreateDirectory(localDir);
                    await File.WriteAllBytesAsync(localPath, data, ct);
                }
            }

            // Pull HQ variant if it exists in manifest
            if (!string.IsNullOrEmpty(entry.HqKey))
            {
                var hqPath = Path.Combine(imageDir, entry.ProjectId, $"{entry.ImageId}.hq.png");
                if (!File.Exists(hqPath))
                {
                    var hqData = await _s3.GetObjectAsync(entry.HqKey, ct);
                    if (hqData is not null)
                    {
                        Directory.CreateDirectory(localDir);
                        await File.WriteAllBytesAsync(hqPath, hqData, ct);
                    }
                }
            }

            completed++;
        }

        progress?.Report(new SyncProgress(SyncPhase.Complete, totalItems, totalItems, "Pull complete"));
    }

    /// <summary>
    /// Test S3 connection by attempting to list objects.
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken ct)
    {
        try
        {
            var prefix = string.IsNullOrEmpty(_config.BasePrefix) ? "" : _config.BasePrefix + "/";
            await _s3.ListObjectsAsync(prefix, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
