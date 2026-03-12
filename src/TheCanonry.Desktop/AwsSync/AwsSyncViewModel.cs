namespace TheCanonry.Desktop.AwsSync;

using System.Windows.Input;
using TheCanonry.Desktop.Shared;

public class AwsSyncViewModel : ViewModelBase
{
    private string _bucketName = "";
    private string _prefix = "";
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

        await Task.Delay(500); // Integration point: test S3 connectivity

        ConnectionStatus = "Connection test placeholder";
        AppendLog("S3 connection test not yet wired to ApiClients.");
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

        try
        {
            // Integration point: iterate images from DB and upload via ApiClients
            await Task.Delay(500);
            AppendLog("Image sync not yet wired to ApiClients.");
        }
        finally
        {
            IsUploading = false;
            SyncProgress = 100;
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

        try
        {
            await Task.Delay(500);
            AppendLog("Catalog upload not yet wired to ApiClients.");
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
