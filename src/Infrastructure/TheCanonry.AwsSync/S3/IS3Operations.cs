namespace TheCanonry.AwsSync.S3;

public interface IS3Operations
{
    Task<byte[]?> GetObjectAsync(string key, CancellationToken ct);

    Task PutObjectAsync(
        string key,
        byte[] data,
        string contentType,
        IDictionary<string, string>? tags = null,
        string? cacheControl = null,
        CancellationToken ct = default);

    Task<bool> ObjectExistsAsync(string key, CancellationToken ct);

    Task<IReadOnlyList<string>> ListObjectsAsync(string prefix, CancellationToken ct);

    Task DeleteObjectAsync(string key, CancellationToken ct);
}
