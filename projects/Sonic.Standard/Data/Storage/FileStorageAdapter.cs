using System.Text.Json;
using Std.DataStorage;

namespace Std.Data.Storage;

/// <summary>
///     基于文件系统的存储适配器，使用 JSON 序列化将值持久化到文件。
/// </summary>
public sealed class FileStorageAdapter : IFileStorage
{
    private readonly string _base_path;
    private readonly JsonSerializerOptions _json_options;

    /// <summary>
    ///     初始化文件存储适配器的新实例。
    /// </summary>
    /// <param name="basePath">文件存储根目录路径。</param>
    public FileStorageAdapter(string basePath)
    {
        _base_path = basePath;
        _json_options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    /// <summary>
    ///     将值写入指定路径的文件。
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="path">文件路径。</param>
    /// <param name="value">要写入的值。</param>
    public void write<T>(string path, in T value)
    {
        var fullPath = resolve_path(path);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(value, _json_options);
        File.WriteAllText(fullPath, json);
    }

    /// <summary>
    ///     从指定路径的文件读取值。
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="path">文件路径。</param>
    /// <returns>读取到的值。</returns>
    public T read<T>(string path)
    {
        var fullPath = resolve_path(path);
        var json = File.ReadAllText(fullPath);
        return JsonSerializer.Deserialize<T>(json, _json_options)!;
    }

    /// <summary>
    ///     解析文件路径，将相对路径转换为基于根目录的绝对路径。
    /// </summary>
    /// <param name="path">原始路径。</param>
    /// <returns>绝对路径。</returns>
    private string resolve_path(string path)
    {
        if (Path.IsPathRooted(path)) return path;

        return Path.Combine(_base_path, path);
    }
}