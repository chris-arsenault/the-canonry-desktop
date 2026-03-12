namespace TheCanonry.AwsSync.Types;

public enum SyncPhase
{
    Scanning,
    Uploading,
    GeneratingVariants,
    UploadingVariants,
    UploadingHq,
    UpdatingManifest,
    BuildingCatalog,
    Complete,
    Failed
}

public sealed record SyncProgress(
    SyncPhase Phase,
    int TotalItems,
    int CompletedItems,
    string CurrentItem,
    string? ErrorMessage = null);
