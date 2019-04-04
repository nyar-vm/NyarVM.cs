using System.Reflection;
using Core.Plugin;

namespace Std.Plugin;

/// <summary>
///     插件宿主，实现 IPluginHost 接口，管理插件的加载和扩展点查询
/// </summary>
public sealed class PluginHost : IPluginHost
{
    /// <summary>
    ///     已加载的插件列表
    /// </summary>
    private readonly List<IPlugin> _plugins = [];

    /// <summary>
    ///     从指定路径加载插件
    /// </summary>
    /// <param name="path">插件程序集路径</param>
    public void load(string path)
    {
        var assembly = Assembly.LoadFrom(path);
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

        foreach (var type in pluginTypes)
            if (Activator.CreateInstance(type) is IPlugin plugin)
            {
                plugin.initialize(this);
                _plugins.Add(plugin);
            }
    }

    /// <summary>
    ///     获取所有已加载插件
    /// </summary>
    /// <returns>已加载插件的只读列表</returns>
    public IReadOnlyList<IPlugin> get_plugins()
    {
        return _plugins.AsReadOnly();
    }

    /// <summary>
    ///     获取指定扩展点的所有扩展
    /// </summary>
    /// <typeparam name="T">扩展点类型</typeparam>
    /// <returns>扩展实例的只读列表</returns>
    public IReadOnlyList<T> get_extensions<T>()
    {
        var result = new List<T>();
        foreach (var plugin in _plugins)
            if (plugin is T extension)
                result.Add(extension);

        return result.AsReadOnly();
    }
}