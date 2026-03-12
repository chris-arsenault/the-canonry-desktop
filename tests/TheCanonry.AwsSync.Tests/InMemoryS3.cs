namespace TheCanonry.AwsSync.Tests;

using TheCanonry.AwsSync.S3;

/// <summary>
/// In-memory implementation of <see cref="IS3Operations"/> for testing without AWS.
/// </summary>
public sealed class InMemoryS3 : IS3Operations
{
    private readonly Dictionary<string, (byte[] Data, string ContentType, Dictionary<string, string>? Tags)> _objects = [];

    public Task<byte[]?> GetObjectAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        byte[]? result = _objects.TryGetValue(key, out var obj) ? obj.Data : null;
        return Task.FromResult(result);
    }

    public Task PutObjectAsync(
        string key,
        byte[] data,
        string contentType,
        IDictionary<string, string>? tags = null,
        string? cacheControl = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var tagDict = tags is not null ? new Dictionary<string, string>(tags) : null;
        _objects[key] = (data, contentType, tagDict);
        return Task.CompletedTask;
    }

    public Task<bool> ObjectExistsAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_objects.ContainsKey(key));
    }

    public Task<IReadOnlyList<string>> ListObjectsAsync(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        IReadOnlyList<string> result = _objects.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .Order()
            .ToList();
        return Task.FromResult(result);
    }

    public Task DeleteObjectAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _objects.Remove(key);
        return Task.CompletedTask;
    }

    // ── Test assertion helpers ──────────────────────────────────────────

    public bool Contains(string key) => _objects.ContainsKey(key);

    public byte[]? GetData(string key) =>
        _objects.TryGetValue(key, out var obj) ? obj.Data : null;

    public string? GetContentType(string key) =>
        _objects.TryGetValue(key, out var obj) ? obj.ContentType : null;

    public int Count => _objects.Count;

    public IReadOnlyCollection<string> Keys => _objects.Keys;
}
