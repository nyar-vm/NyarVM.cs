namespace Core.Widget.Style;

/// <summary>
///     IResourceDictionary 接口
/// </summary>
public interface IResourceDictionary
{
    /// <summary>
    ///     获取指定键的资源
    /// </summary>
    object? get_resource(string key);
}