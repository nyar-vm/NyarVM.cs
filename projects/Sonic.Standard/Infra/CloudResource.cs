using Core.Infra;

namespace Std.Infra;

/// <summary>
///     云资源定义，实现 <see cref="ICloudResource" /> 接口，
///     提供资源类型、名称和属性字典的统一描述。
/// </summary>
public sealed class CloudResource : ICloudResource
{
    /// <summary>
    ///     初始化 <see cref="CloudResource" /> 的新实例。
    /// </summary>
    /// <param name="resourceType">资源类型标识。</param>
    /// <param name="name">资源名称。</param>
    /// <param name="properties">资源属性字典。</param>
    public CloudResource(string resourceType, string name, Dictionary<string, object>? properties = null)
    {
        resource_type = resourceType;
        this.name = name;
        this.properties = properties ?? new Dictionary<string, object>();
    }

    /// <summary>
    ///     资源类型标识。
    /// </summary>
    public string resource_type { get; }

    /// <summary>
    ///     资源名称。
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     资源属性字典。
    /// </summary>
    public Dictionary<string, object> properties { get; }

    /// <summary>
    ///     获取资源名称（实现 <see cref="ICloudResource" /> 接口）。
    /// </summary>
    string ICloudResource.resource_name => name;
}