using System.Security.Cryptography;

namespace Hermes.Storage.Local;

public sealed class LocalFileSystemStorage : IObjectStorage
{
    private readonly string _basePath;

    public LocalFileSystemStorage(StorageOptions options)
    {
        _basePath = Path.GetFullPath(options.BasePath ?? Path.Combine(Path.GetTempPath(), "hermes-storage"));
        Directory.CreateDirectory(_basePath);
    }

    public Task<ObjectMetadata?> GetMetadataAsync(string key, CancellationToken ct = default)
    {
        var path = ResolvePath(key);

        if (!File.Exists(path)) return Task.FromResult<ObjectMetadata?>(null);

        var info = new FileInfo(path);

        return Task.FromResult<ObjectMetadata?>(new ObjectMetadata
        {
            Key = key,
            Size = info.Length,
            ContentType = GuessContentType(key),
            LastModified = info.LastWriteTimeUtc,
            ETag = ComputeEtag(path)
        });
    }

    public async Task PutAsync(string key, Stream data, string? contentType = null, CancellationToken ct = default)
    {
        var path = ResolvePath(key);
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        await data.CopyToAsync(fs, ct);
    }

    public Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = ResolvePath(key);

        if (!File.Exists(path)) return Task.FromResult(false);

        File.Delete(path);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var path = ResolvePath(key);
        return Task.FromResult(File.Exists(path));
    }

    public Task<IReadOnlyList<ObjectInfo>> ListAsync(string? prefix = null, CancellationToken ct = default)
    {
        var searchPath = prefix != null ? ResolvePath(prefix) : _basePath;
        var results = new List<ObjectInfo>();

        if (!Directory.Exists(searchPath)) return Task.FromResult<IReadOnlyList<ObjectInfo>>(results);

        var searchDir = Directory.Exists(searchPath) ? searchPath : Path.GetDirectoryName(searchPath)!;
        var pattern = Directory.Exists(searchPath) ? "*" : Path.GetFileName(searchPath) + "*";

        foreach (var file in Directory.EnumerateFiles(searchDir, pattern, SearchOption.AllDirectories))
        {
            var relativeKey = GetRelativeKey(file);
            var info = new FileInfo(file);

            results.Add(new ObjectInfo
            {
                Key = relativeKey,
                Size = info.Length,
                LastModified = info.LastWriteTimeUtc,
                ETag = ComputeEtag(file)
            });
        }

        return Task.FromResult<IReadOnlyList<ObjectInfo>>(results);
    }

    public Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var path = ResolvePath(key);

        if (!File.Exists(path)) throw new FileNotFoundException($"对象不存在：{key}");

        return Task.FromResult($"file:///{path.Replace('\\', '/')}");
    }

    public Task<Stream> GetAsync(string key, CancellationToken ct = default)
    {
        var path = ResolvePath(key);

        if (!File.Exists(path)) throw new FileNotFoundException($"对象不存在：{key}");

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
        return Task.FromResult(stream);
    }

    private string ResolvePath(string key)
    {
        var normalized = key.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, normalized));

        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"路径越界：{key}");

        return fullPath;
    }

    private string GetRelativeKey(string fullPath)
    {
        return fullPath[_basePath.Length..].TrimStart(Path.DirectorySeparatorChar).Replace('\\', '/');
    }

    private static string ComputeEtag(string path)
    {
        using var fs = File.OpenRead(path);
        var hash = SHA256.HashData(fs);
        return $"\"{Convert.ToHexString(hash)[..16]}\"";
    }

    private static string GuessContentType(string key)
    {
        var ext = Path.GetExtension(key).ToLowerInvariant();
        return ext switch
        {
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".txt" => "text/plain",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".bin" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }
}