using Std.Config.Node;

namespace Std.Config.Provider;

/// <summary>
///     可选包装器，将内部配置源的加载异常捕获并返回空节点�?///
/// </summary>
public sealed class OptionalConfigProvider : IConfigProvider
{
    private readonly IConfigProvider _inner;

    /// <summary>
    ///     使用内部配置源初始化 <see cref="OptionalConfigProvider" /> 的新实例�?    ///
    /// </summary>
    /// <param name="inner">被包装的内部配置源�?/param>
    public OptionalConfigProvider(IConfigProvider inner)
    {
        _inner = inner;
    }

    /// <summary>
    ///     加载并返回配置树根节点�?    /// 当内部配置源加载失败时，返回 <see cref="NullConfigNode.instance" /> 而非抛出异常�?    ///
    /// </summary>
    /// <returns>配置树的根节点，或空节点�?/returns>
    public ConfigNode load()
    {
        try
        {
            return _inner.load();
        }
        catch (FileNotFoundException)
        {
            return NullConfigNode.instance;
        }
    }
}