using System.Reflection;

namespace Nyar.PackageRegistry;

/// <summary>
///     注册表工厂，提供非 DI 场景下的注册表实例创建能力
///     通过反射创建具体实现，保持抽象层对实现层的隔离
/// </summary>
public static class RegistryFactory
{
    /// <summary>
    ///     创建指定名称的注册表实例
    /// </summary>
    /// <param name="registryName">注册表名称</param>
    /// <param name="endpoint">自定义端点地址，null 使用默认值</param>
    /// <returns>注册表实例</returns>
    /// <exception cref="ArgumentException">不支持的注册表名称</exception>
    public static IRegistry create(string registryName, string? endpoint = null)
    {
        var (typeName, assemblyName) = registryName.ToLowerInvariant() switch
        {
            "npm" => ("Legion.NpmRegistry", "Nyar.PackageManager.Registry.Npm"),
            "jsr" => ("Legion.JsrRegistry", "Nyar.PackageManager.Registry.Jsr"),
            "conda" => ("Legion.CondaRegistry", "Nyar.PackageManager.Registry.Conda"),
            "maven" => ("Legion.MavenRegistry", "Nyar.PackageManager.Registry.Maven"),
            "nuget" => ("Legion.NuGetRegistry", "Nyar.PackageManager.Registry.Nuget"),
            "valhalla" => ("Legion.ValhallaRegistry", "Nyar.PackageManager.Registry.Valhalla"),
            _ => throw new ArgumentException($"不支持的注册表类型: {registryName}")
        };

        var assembly = Assembly.Load(assemblyName);
        var type = assembly.GetType(typeName)
                   ?? throw new ArgumentException($"在程序集 {assemblyName} 中未找到类型 {typeName}");

        if (Activator.CreateInstance(type) is not IRegistry registry)
            throw new RegistryException($"无法创建注册表实例: {registryName}");

        if (endpoint is not null) registry.endpoint = endpoint;

        return registry;
    }

    /// <summary>
    ///     创建所有默认注册表实例
    /// </summary>
    /// <returns>注册表名称到实例的映射</returns>
    public static Dictionary<string, IRegistry> create_all_defaults()
    {
        var names = new[] { "npm", "jsr", "conda", "maven", "nuget", "valhalla" };
        var result = new Dictionary<string, IRegistry>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in names)
            try
            {
                var registry = create(name);
                result[registry.name] = registry;
            }
            catch
            {
                // 插件不可用时跳过
            }

        return result;
    }
}