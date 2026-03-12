namespace TheCanonry.AwsSync.Types;

/// <summary>
/// AWS S3 sync configuration.
/// </summary>
public sealed record AwsSyncConfig(
    string Region,
    string BucketName,
    string BasePrefix,
    string? ProfileName,
    string? AccessKeyId,
    string? SecretAccessKey,
    string? CdnBaseUrl);
