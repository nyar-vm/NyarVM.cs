namespace Std.Terminal.Controls;

/// <summary>
///     文件系统条目信息
/// </summary>
public sealed class FileEntry
{
    /// <summary>创建文件条目</summary>
    /// <param name="fullPath">完整路径</param>
    /// <param name="name">显示名称</param>
    /// <param name="isDirectory">是否为目录</param>
    /// <param name="size">文件大小</param>
    /// <param name="lastModified">最后修改时间</param>
    public FileEntry(string fullPath, string name, bool isDirectory, long size = 0, DateTime lastModified = default)
    {
        full_path = fullPath;
        this.name = name;
        is_directory = isDirectory;
        this.size = size;
        last_modified = lastModified;
    }

    /// <summary>完整路径</summary>
    public string full_path { get; }

    /// <summary>显示名称</summary>
    public string name { get; }

    /// <summary>是否为目录</summary>
    public bool is_directory { get; }

    /// <summary>文件大小（字节，仅文件有效）</summary>
    public long size { get; }

    /// <summary>最后修改时间</summary>
    public DateTime last_modified { get; }
}