namespace TheCanonry.AwsSync.Types;

/// <summary>
/// AWS S3 sync configuration.
/// </summary>
// CdnBaseUrl is used in string interpolation throughout the codebase; converting to Uri would
// require .ToString() at every use site with no benefit — suppress the URI string rule.
#pragma warning disable CA1054, CA1056 // URI-like properties should not be strings
public sealed record AwsSyncConfig(
    string Region,
    string BucketName,
    string BasePrefix,
    string? ProfileName,
    string? AccessKeyId,
    string? SecretAccessKey,
    string? CdnBaseUrl);
#pragma warning restore CA1054, CA1056
