using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using TheCanonry.AwsSync.Types;

namespace TheCanonry.AwsSync.S3;

public sealed class S3Operations : IS3Operations, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;

    public S3Operations(AwsSyncConfig config)
    {
        var s3Config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(config.Region)
        };

        if (!string.IsNullOrEmpty(config.AccessKeyId) && !string.IsNullOrEmpty(config.SecretAccessKey))
        {
            _client = new AmazonS3Client(
                new BasicAWSCredentials(config.AccessKeyId, config.SecretAccessKey),
                s3Config);
        }
        else if (!string.IsNullOrEmpty(config.ProfileName))
        {
            var chain = new CredentialProfileStoreChain();
            if (chain.TryGetAWSCredentials(config.ProfileName, out var credentials))
                _client = new AmazonS3Client(credentials, s3Config);
            else
                throw new InvalidOperationException($"AWS profile '{config.ProfileName}' not found");
        }
        else
        {
            _client = new AmazonS3Client(s3Config);
        }

        _bucket = config.BucketName;
    }

    public async Task<byte[]?> GetObjectAsync(string key, CancellationToken ct)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucket,
                Key = key
            };

            using var response = await _client.GetObjectAsync(request, ct);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task PutObjectAsync(
        string key,
        byte[] data,
        string contentType,
        IDictionary<string, string>? tags = null,
        string? cacheControl = null,
        CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            ContentType = contentType,
            InputStream = new MemoryStream(data)
        };

        if (cacheControl is not null)
            request.Headers.CacheControl = cacheControl;

        if (tags is { Count: > 0 })
        {
            request.TagSet = tags
                .Select(kvp => new Tag { Key = kvp.Key, Value = kvp.Value })
                .ToList();
        }

        await _client.PutObjectAsync(request, ct);
    }

    public async Task<bool> ObjectExistsAsync(string key, CancellationToken ct)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucket,
                Key = key
            };
            await _client.GetObjectMetadataAsync(request, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> ListObjectsAsync(string prefix, CancellationToken ct)
    {
        var keys = new List<string>();
        string? continuationToken = null;

        do
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucket,
                Prefix = prefix,
                ContinuationToken = continuationToken
            };

            var response = await _client.ListObjectsV2Async(request, ct);

            foreach (var obj in response.S3Objects)
                keys.Add(obj.Key);

            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);

        return keys;
    }

    public async Task DeleteObjectAsync(string key, CancellationToken ct)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucket,
            Key = key
        };

        await _client.DeleteObjectAsync(request, ct);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
