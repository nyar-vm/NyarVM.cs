using System.Collections.Generic;

namespace Core.Plugin;

/// <summary>
///     IPluginHost 接口
/// </summary>
public interface IPluginHost
{
    /// <summary>
    ///     从指定路径加载插件
    /// </summary>
    void load(string path);

    /// <summary>
    ///     获取所有已加载插件
    /// </summary>
    IReadOnlyList<IPlugin> get_plugins();

    /// <summary>
    ///     获取指定扩展点的所有扩展
    /// </summary>
    IReadOnlyList<T> get_extensions<T>();
}