namespace TheCanonry.Desktop.AwsSync;

using System.Windows.Input;
using TheCanonry.AwsSync.S3;
using TheCanonry.AwsSync.Sync;
using TheCanonry.AwsSync.Types;
using TheCanonry.Desktop.Shared;

public class AwsSyncViewModel : ViewModelBase
{
    private string _bucketName = "";
    private string _prefix = "";
    private string _imageDir = "";
    private string _projectId = "";
    private double _syncProgress;
    private int _imageCount;
    private bool _isUploading;
    private string _syncLog = "";
    private string _connectionStatus = "Not tested";

    public AwsSyncViewModel()
    {
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync, () => !IsUploading);
        SyncImagesCommand = new AsyncRelayCommand(SyncImagesAsync, () => !IsUploading);
        UploadCatalogCommand = new AsyncRelayCommand(UploadCatalogAsync, () => !IsUploading);
    }

    public string BucketName
    {
        get => _bucketName;
        set => SetProperty(ref _bucketName, value);
    }

    public string Prefix
    {
        get => _prefix;
        set => SetProperty(ref _prefix, value);
    }

    /// <summary>Local directory containing image files to sync.</summary>
    public string ImageDir
    {
        get => _imageDir;
        set => SetProperty(ref _imageDir, value);
    }

    /// <summary>Project ID used as the sub-directory within ImageDir.</summary>
    public string ProjectId
    {
        get => _projectId;
        set => SetProperty(ref _projectId, value);
    }

    public double SyncProgress
    {
        get => _syncProgress;
        private set => SetProperty(ref _syncProgress, value);
    }

    public int ImageCount
    {
        get => _imageCount;
        private set => SetProperty(ref _imageCount, value);
    }

    public bool IsUploading
    {
        get => _isUploading;
        private set
        {
            if (SetProperty(ref _isUploading, value))
            {
                ((AsyncRelayCommand)TestConnectionCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)SyncImagesCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)UploadCatalogCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string SyncLog
    {
        get => _syncLog;
        private set => SetProperty(ref _syncLog, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetProperty(ref _connectionStatus, value);
    }

    public ICommand TestConnectionCommand { get; }
    public ICommand SyncImagesCommand { get; }
    public ICommand UploadCatalogCommand { get; }

    private async Task TestConnectionAsync()
    {
        AppendLog("Testing S3 connection...");
        ConnectionStatus = "Testing...";

        if (string.IsNullOrWhiteSpace(BucketName))
        {
            ConnectionStatus = "Bucket name required";
            AppendLog("ERROR: Bucket name is required.");
            return;
        }

        // TODO: Support region configuration in the UI.
        var config = new AwsSyncConfig(
            Region: "us-east-1",
            BucketName: BucketName,
            BasePrefix: Prefix,
            ProfileName: null,
            AccessKeyId: null,
            SecretAccessKey: null,
            CdnBaseUrl: null);

        try
        {
            using var s3 = new S3Operations(config);
            var service = new ImageSyncService(s3, config);
            var ok = await service.TestConnectionAsync(CancellationToken.None);

            ConnectionStatus = ok ? "Connected" : "Connection failed";
            AppendLog(ok ? "S3 connection successful." : "S3 connection failed — check bucket name and credentials.");
        }
        catch (Exception ex)
        {
            ConnectionStatus = "Error";
            AppendLog($"ERROR testing connection: {ex.Message}");
        }
    }

    private async Task SyncImagesAsync()
    {
        if (string.IsNullOrWhiteSpace(BucketName))
        {
            AppendLog("Bucket name is required.");
            return;
        }

        IsUploading = true;
        SyncProgress = 0;
        AppendLog($"Starting image sync to s3://{BucketName}/{Prefix}...");

        var config = new AwsSyncConfig(
            Region: "us-east-1",
            BucketName: BucketName,
            BasePrefix: Prefix,
            ProfileName: null,
            AccessKeyId: null,
            SecretAccessKey: null,
            CdnBaseUrl: null);

        try
        {
            using var s3 = new S3Operations(config);
            var service = new ImageSyncService(s3, config);

            var imageDir = string.IsNullOrWhiteSpace(ImageDir) ? "." : ImageDir;
            var projectId = string.IsNullOrWhiteSpace(ProjectId) ? "default" : ProjectId;

            var progress = new Progress<SyncProgress>(p =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (p.TotalItems > 0)
                        SyncProgress = (double)p.CompletedItems / p.TotalItems * 100;
                    ImageCount = p.CompletedItems;
                    SyncLog += $"[{DateTime.Now:HH:mm:ss}] [{p.Phase}] {p.CurrentItem}\n";
                });
            });

            var manifest = await service.SyncAsync(imageDir, projectId, progress);
            AppendLog($"Sync complete — {manifest.Entries.Count} images in manifest.");
            SyncProgress = 100;
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR during sync: {ex.Message}");
        }
        finally
        {
            IsUploading = false;
        }
    }

    private async Task UploadCatalogAsync()
    {
        if (string.IsNullOrWhiteSpace(BucketName))
        {
            AppendLog("Bucket name is required.");
            return;
        }

        IsUploading = true;
        AppendLog($"Uploading catalog to s3://{BucketName}/{Prefix}catalog.json...");

        var config = new AwsSyncConfig(
            Region: "us-east-1",
            BucketName: BucketName,
            BasePrefix: Prefix,
            ProfileName: null,
            AccessKeyId: null,
            SecretAccessKey: null,
            CdnBaseUrl: null);

        try
        {
            using var s3 = new S3Operations(config);
            var service = new ImageSyncService(s3, config);

            // Load the current manifest via a minimal sync that only reads it,
            // then rebuild the catalog from the existing manifest state.
            // We use TestConnectionAsync to verify connectivity, then build.
            var connected = await service.TestConnectionAsync(CancellationToken.None);
            if (!connected)
            {
                AppendLog("ERROR: Cannot connect to S3. Check credentials and bucket name.");
                return;
            }

            var catalogBuilder = new CatalogBuilder(s3, Prefix);

            // Load the manifest so we can rebuild the catalog from it.
            var manifestManager = new ManifestManager(s3, Prefix);
            var manifest = await manifestManager.LoadAsync(CancellationToken.None);

            await catalogBuilder.BuildAndUploadAsync(manifest, CancellationToken.None);
            AppendLog($"Catalog uploaded with {manifest.Entries.Count} entries.");
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR uploading catalog: {ex.Message}");
        }
        finally
        {
            IsUploading = false;
        }
    }

    private void AppendLog(string message)
    {
        SyncLog += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
    }
}
