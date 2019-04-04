namespace Hermes.Storage;

public interface IObjectStorage
{
    Task<ObjectMetadata?> GetMetadataAsync(string key, CancellationToken ct = default);

    Task<System.IO.Stream> GetAsync(string key, CancellationToken ct = default);

    Task PutAsync(string key, Stream data, string? contentType = null, CancellationToken ct = default);

    Task<bool> DeleteAsync(string key, CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    Task<IReadOnlyList<ObjectInfo>> ListAsync(string? prefix = null, CancellationToken ct = default);

    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default);
}

public sealed class ObjectMetadata
{
    public string Key { get; init; }
    public long Size { get; init; }
    public string? ContentType { get; init; }
    public string? ETag { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class ObjectInfo
{
    public string Key { get; init; }
    public long Size { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public string? ETag { get; init; }
}

public sealed class StorageOptions
{
    public string? Endpoint { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? Bucket { get; set; }
    public string? Region { get; set; }
    public string? BasePath { get; set; }
    public bool UseHttps { get; set; } = true;
}