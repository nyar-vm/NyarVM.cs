using System.Security.Cryptography;
using Core.Data.Storage;

namespace Std.Data;

/// <summary>
///     本地文件系统存储配置选项。
/// </summary>
public sealed class LocalStorageOptions
{
    /// <summary>
    ///     存储根目录路径。
    /// </summary>
    public string base_path { get; set; } = "./storage";

    /// <summary>
    ///     公开访问的基础 URL。
    /// </summary>
    public string? public_base_url { get; set; }
}

/// <summary>
///     本地文件系统存储提供者，实现 <see cref="IStorageProvider" /> 接口。
/// </summary>
public sealed class LocalStorageProvider : IStorageProvider
{
    private readonly string _base_path;
    private readonly LocalStorageOptions _options;

    /// <summary>
    ///     初始化本地存储提供者。
    /// </summary>
    /// <param name="options">本地存储配置选项</param>
    public LocalStorageProvider(LocalStorageOptions options)
    {
        _options = options;
        _base_path = Path.GetFullPath(options.base_path);

        if (!Directory.Exists(_base_path)) Directory.CreateDirectory(_base_path);
    }

    /// <inheritdoc />
    public Task<StorageFileInfo> UploadAsync(string key, System.IO.Stream data, string? contentType = null,
        CancellationToken ct = default)
    {
        var filePath = get_file_path(key);
        ensure_directory_exists(filePath);

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        data.CopyTo(fs);

        return Task.FromResult(create_file_info(key, filePath, contentType));
    }

    /// <inheritdoc />
    public async Task<StorageFileInfo> UploadAsync(string key, byte[] data, string? contentType = null,
        CancellationToken ct = default)
    {
        var filePath = get_file_path(key);
        ensure_directory_exists(filePath);

        await File.WriteAllBytesAsync(filePath, data, ct);
        return create_file_info(key, filePath, contentType);
    }

    /// <inheritdoc />
    public Task<System.IO.Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        var filePath = get_file_path(key);
        if (!File.Exists(filePath)) throw new FileNotFoundException($"文件不存在: {key}");

        return Task.FromResult<System.IO.Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
    }

    /// <inheritdoc />
    public async Task<byte[]> DownloadBytesAsync(string key, CancellationToken ct = default)
    {
        var filePath = get_file_path(key);
        if (!File.Exists(filePath)) throw new FileNotFoundException($"文件不存在: {key}");

        return await File.ReadAllBytesAsync(filePath, ct);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(File.Exists(get_file_path(key)));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var filePath = get_file_path(key);
        if (File.Exists(filePath)) File.Delete(filePath);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<StorageFileInfo?> GetFileInfoAsync(string key, CancellationToken ct = default)
    {
        var filePath = get_file_path(key);
        if (!File.Exists(filePath)) return Task.FromResult<StorageFileInfo?>(null);

        return Task.FromResult<StorageFileInfo?>(create_file_info(key, filePath));
    }

    /// <inheritdoc />
    public string GetUrl(string key, TimeSpan? expire = null)
    {
        if (!string.IsNullOrEmpty(_options.public_base_url)) return $"{_options.public_base_url.TrimEnd('/')}/{key}";

        return Path.GetFullPath(get_file_path(key));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ListAsync(string? prefix = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var searchPath = string.IsNullOrEmpty(prefix) ? _base_path : Path.Combine(_base_path, prefix);
        var searchDir = Path.GetDirectoryName(searchPath) ?? _base_path;
        var searchPattern = Path.GetFileName(searchPath);

        if (string.IsNullOrEmpty(searchPattern) || searchPattern == prefix) searchPattern = "*";

        if (!Directory.Exists(searchDir)) yield break;

        var files = Directory.GetFiles(searchDir, searchPattern, SearchOption.AllDirectories);
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(_base_path, file).Replace('\\', '/');
            yield return relativePath;
            await Task.Yield();
        }
    }

    /// <inheritdoc />
    public Task<StorageFileInfo> CopyAsync(string sourceKey, string destKey, CancellationToken ct = default)
    {
        var sourcePath = get_file_path(sourceKey);
        var destPath = get_file_path(destKey);

        ensure_directory_exists(destPath);

        File.Copy(sourcePath, destPath, true);
        return Task.FromResult(create_file_info(destKey, destPath));
    }

    private string get_file_path(string key)
    {
        return Path.Combine(_base_path, key.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void ensure_directory_exists(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    private StorageFileInfo create_file_info(string key, string filePath, string? contentType = null)
    {
        var fileInfo = new FileInfo(filePath);
        return new StorageFileInfo
        {
            Key = key,
            Url = GetUrl(key),
            Size = fileInfo.Exists ? fileInfo.Length : 0,
            ContentType = contentType ?? infer_content_type(key),
            LastModified = fileInfo.Exists ? fileInfo.LastWriteTimeUtc : null,
            ETag = fileInfo.Exists ? compute_e_tag(filePath) : string.Empty
        };
    }

    private static string infer_content_type(string key)
    {
        var ext = Path.GetExtension(key).ToLower();
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
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    private static string compute_e_tag(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        var hash = SHA256.HashData(fs);
        return Convert.ToHexString(hash)[..16].ToLower();
    }
}