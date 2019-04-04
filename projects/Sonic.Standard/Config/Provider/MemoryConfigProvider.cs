using Std.Config.Node;

namespace Std.Config.Provider;

/// <summary>
///     基于内存字典的配置源，将扁平键值对转换为层级化�?ConfigNode 树�?/// 使用冒号 : 作为嵌套路径分隔符�?///
/// </summary>
public sealed class MemoryConfigProvider : IConfigProvider
{
    private readonly IDictionary<string, string> _data;

    /// <summary>
    ///     使用指定的键值对字典初始�?<see cref="MemoryConfigProvider" /> 的新实例�?    ///
    /// </summary>
    /// <param name="data">键值对字典，支持冒号分隔的嵌套路径�?/param>
    public MemoryConfigProvider(IDictionary<string, string> data)
    {
        _data = data;
    }

    /// <summary>
    ///     加载并返回配置树根节点�?    ///
    /// </summary>
    /// <returns>配置树的根节点�?/returns>
    public ConfigNode load()
    {
        var builder = new ConfigNodeBuilder();

        foreach (var kvp in _data)
        {
            var path = kvp.Key.Replace(':', '.');
            builder.add(path, kvp.Value);
        }

        return builder.build();
    }
}