using RegistryPackage = Nyar.PackageRegistry.Package;

namespace Nyar.PackageManager.Package;

/// <summary>
///     包信息摘要
/// </summary>
public class PackageInfo
{
    /// <summary>
    ///     包名
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     版本
    /// </summary>
    public string version { get; set; } = string.Empty;

    /// <summary>
    ///     最新版本
    /// </summary>
    public string latest_version { get; set; } = string.Empty;

    /// <summary>
    ///     许可证
    /// </summary>
    public string license { get; set; } = "unknown";

    /// <summary>
    ///     描述
    /// </summary>
    public string description { get; set; } = string.Empty;

    /// <summary>
    ///     作者
    /// </summary>
    public string author { get; set; } = string.Empty;

    /// <summary>
    ///     是否已安装
    /// </summary>
    public bool is_installed { get; set; }

    /// <summary>
    ///     依赖列表
    /// </summary>
    public List<string> dependencies { get; set; } = [];

    /// <summary>
    ///     依赖版本映射
    /// </summary>
    public Dictionary<string, string> dependency_versions { get; set; } = new();

    /// <summary>
    ///     同伴依赖
    /// </summary>
    public Dictionary<string, string>? peer_dependencies { get; set; }

    /// <summary>
    ///     从注册表包元数据隐式转换为包信息摘要
    /// </summary>
    public static implicit operator PackageInfo?(RegistryPackage? pkg)
    {
        if (pkg is null) return null;

        return new PackageInfo
        {
            name = pkg.name,
            version = pkg.version,
            latest_version = pkg.version,
            license = pkg.license ?? "unknown",
            description = pkg.description ?? string.Empty,
            author = pkg.author ?? string.Empty,
            dependencies = pkg.dependencies,
            dependency_versions = pkg.dependency_versions,
            peer_dependencies = pkg.peer_dependencies
        };
    }

    /// <summary>
    ///     从注册表包列表转换为包信息列表
    /// </summary>
    public static List<PackageInfo> from_registry_packages(List<RegistryPackage> packages)
    {
        return [.. packages.Select(p => (PackageInfo)p)];
    }

    /// <summary>
    ///     转换为注册表包元数据
    /// </summary>
    public RegistryPackage to_registry_package()
    {
        return new RegistryPackage
        {
            name = name,
            version = version,
            license = license == "unknown" ? null : license,
            description = description,
            author = author,
            dependencies = dependencies,
            dependency_versions = dependency_versions,
            peer_dependencies = peer_dependencies
        };
    }
}