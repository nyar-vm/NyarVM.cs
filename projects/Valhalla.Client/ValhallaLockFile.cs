using System.Text.Json;

namespace Valhalla.Client;

/// <summary>
///     protoswap.lock 文件读写，作为客户端本地信任锚点
/// </summary>
public class ValhallaLockFile
{
    private readonly string _project_directory;
    private ValhallaLockContent? _content;

    /// <summary>
    ///     创建锁文件实例
    /// </summary>
    /// <param name="projectDirectory">项目目录，lock 文件位于此目录下</param>
    public ValhallaLockFile(string projectDirectory)
    {
        _project_directory = projectDirectory;
    }

    /// <summary>
    ///     lock 文件完整路径
    /// </summary>
    public string file_path => Path.Combine(_project_directory, "protoswap.lock");

    /// <summary>
    ///     lock 文件是否存在
    /// </summary>
    public bool exists()
    {
        return File.Exists(file_path);
    }

    /// <summary>
    ///     从磁盘加载 lock 文件
    /// </summary>
    public async Task load()
    {
        if (!File.Exists(file_path))
        {
            _content = new ValhallaLockContent();
            return;
        }

        var json = await File.ReadAllTextAsync(file_path);
        _content = JsonSerializer.Deserialize<ValhallaLockContent>(json)
                   ?? new ValhallaLockContent();
    }

    /// <summary>
    ///     保存 lock 到磁盘
    /// </summary>
    public async Task save()
    {
        _content ??= new ValhallaLockContent();
        _content.updated_at = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(_content,
            new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(file_path, json);
    }

    /// <summary>
    ///     删除 lock 文件
    /// </summary>
    public void delete()
    {
        if (File.Exists(file_path)) File.Delete(file_path);

        _content = new ValhallaLockContent();
    }

    /// <summary>
    ///     添加或更新一个包条目
    /// </summary>
    public void add_or_update(string packageName, ValhallaLockEntry entry)
    {
        _content ??= new ValhallaLockContent();
        _content.entries[packageName] = entry;
        _content.updated_at = DateTime.UtcNow;
    }

    /// <summary>
    ///     删除指定包的 lock 条目
    /// </summary>
    public bool remove(string packageName)
    {
        if (_content is null) return false;

        var removed = _content.entries.Remove(packageName);
        if (removed) _content.updated_at = DateTime.UtcNow;

        return removed;
    }

    /// <summary>
    ///     获取指定包的 lock 条目
    /// </summary>
    public ValhallaLockEntry? get_entry(string packageName)
    {
        if (_content?.entries.TryGetValue(packageName, out var entry) == true) return entry;

        return null;
    }

    /// <summary>
    ///     获取所有 lock 条目
    /// </summary>
    public Dictionary<string, ValhallaLockEntry> get_all_entries()
    {
        return _content?.entries ?? new Dictionary<string, ValhallaLockEntry>();
    }

    /// <summary>
    ///     检查指定包的指定版本是否已被 lock
    /// </summary>
    public bool is_version_locked(string packageName, string version)
    {
        var entry = get_entry(packageName);
        return entry is not null && entry.version == version;
    }

    /// <summary>
    ///     获取锁文件中记录的 SHA-256（若有）
    /// </summary>
    public string? get_locked_sha256(string packageName, string version)
    {
        var entry = get_entry(packageName);
        if (entry is not null && entry.version == version) return entry.sha256;

        return null;
    }

    /// <summary>
    ///     获取锁文件中记录的 incarnation（若有）
    /// </summary>
    public int? get_locked_incarnation(string packageName)
    {
        var entry = get_entry(packageName);
        return entry?.incarnation;
    }

    /// <summary>
    ///     获取锁文件中记录的 publisher 指纹（若有）
    /// </summary>
    public string? get_locked_publisher(string packageName)
    {
        var entry = get_entry(packageName);
        return entry?.publisher;
    }

    /// <summary>
    ///     获取 lock 内容（不包括 Entries 字典，用于外部访问）
    /// </summary>
    public ValhallaLockContent? get_content()
    {
        return _content;
    }
}