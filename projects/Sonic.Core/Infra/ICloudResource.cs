namespace Core.Infra;

/// <summary>
///     云资源接口，提供资源名称标识
/// </summary>
public interface ICloudResource
{
    /// <summary>
    ///     资源名称
    /// </summary>
    string resource_name { get; }
}