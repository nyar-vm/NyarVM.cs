namespace Valhalla.Server.Storage;

/// <summary>
///     本地文件系统存储后端实现
/// </summary>
public class LocalStorage : IStorage
{
    private readonly string _root_path;

    /// <summary>
    ///     创建本地存储
    /// </summary>
    /// <param name="rootPath">存储根目录路径</param>
    public LocalStorage(string rootPath)
    {
        _root_path = Path.GetFullPath(rootPath);
        if (!Directory.Exists(_root_path)) Directory.CreateDirectory(_root_path);
    }

    /// <inheritdoc />
    public Task<string?> read_string(string path, CancellationToken ct = default)
    {
        var fullPath = get_full_path(path);
        if (!File.Exists(fullPath)) return Task.FromResult<string?>(null);

        return File.ReadAllTextAsync(fullPath, ct)!;
    }

    /// <inheritdoc />
    public Task<byte[]?> read_bytes(string path, CancellationToken ct = default)
    {
        var fullPath = get_full_path(path);
        if (!File.Exists(fullPath)) return Task.FromResult<byte[]?>(null);

        return File.ReadAllBytesAsync(fullPath, ct)!;
    }

    /// <inheritdoc />
    public async Task<StorageResult> write_string(string path, string content, CancellationToken ct = default)
    {
        try
        {
            var fullPath = get_full_path(path);
            var dir = Path.GetDirectoryName(fullPath);
            if (dir is not null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(fullPath, content, ct);
            return StorageResult.succeed();
        }
        catch (Exception ex)
        {
            return StorageResult.fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<StorageResult> write_bytes(string path, byte[] data, CancellationToken ct = default)
    {
        try
        {
            var fullPath = get_full_path(path);
            var dir = Path.GetDirectoryName(fullPath);
            if (dir is not null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            await File.WriteAllBytesAsync(fullPath, data, ct);
            return StorageResult.succeed();
        }
        catch (Exception ex)
        {
            return StorageResult.fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<bool> exists(string path, CancellationToken ct = default)
    {
        var fullPath = get_full_path(path);
        return Task.FromResult(File.Exists(fullPath) || Directory.Exists(fullPath));
    }

    /// <inheritdoc />
    public Task<StorageResult> delete(string path, CancellationToken ct = default)
    {
        try
        {
            var fullPath = get_full_path(path);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            else if (Directory.Exists(fullPath)) Directory.Delete(fullPath, true);

            return Task.FromResult(StorageResult.succeed());
        }
        catch (Exception ex)
        {
            return Task.FromResult(StorageResult.fail(ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<List<string>> list(string prefix, CancellationToken ct = default)
    {
        var fullPath = get_full_path(prefix);
        if (!Directory.Exists(fullPath)) return Task.FromResult(new List<string>());

        var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
        var result = new List<string>();
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(_root_path, file)
                .Replace('\\', '/');
            result.Add(relativePath);
        }

        return Task.FromResult(result);
    }

    /// <summary>
    ///     获取绝对路径，防止路径穿越
    /// </summary>
    private string get_full_path(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Combine(_root_path, normalized));

        if (!fullPath.StartsWith(_root_path, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"路径穿越检测：{relativePath}");

        return fullPath;
    }
}