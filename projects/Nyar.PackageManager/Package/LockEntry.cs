namespace Nyar.PackageManager.Package;

public class LockEntry
{
    /// <summary>
    ///     包名称
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     版本号
    /// </summary>
    public string version { get; set; } = string.Empty;

    /// <summary>
    ///     来源注册表名称
    /// </summary>
    public string registry { get; set; } = string.Empty;

    /// <summary>
    ///     解析后的下载地址
    /// </summary>
    public string resolved { get; set; } = string.Empty;

    /// <summary>
    ///     完整性校验哈希（格式：sha512-Base64）
    /// </summary>
    public string integrity { get; set; } = string.Empty;

    /// <summary>
    ///     包的直接依赖列表
    /// </summary>
    public List<string> dependencies { get; set; } = [];

    /// <summary>
    ///     包的许可协议
    /// </summary>
    public string license { get; set; } = string.Empty;

    /// <summary>
    ///     是否为开发依赖
    /// </summary>
    public bool is_dev { get; set; }

    /// <summary>
    ///     是否为工作区内部依赖（workspace:* 协议）
    /// </summary>
    public bool is_workspace { get; set; }

    /// <summary>
    ///     下载后的本地安装路径（相对于项目根目录）
    /// </summary>
    public string install_path { get; set; } = string.Empty;
}