using Std.Config.Node;

namespace Std.Config;

/// <summary>
///     配置源抽象接口，每个配置源实现该接口返回 ConfigNode 根节点�?///
/// </summary>
public interface IConfigProvider
{
    /// <summary>
    ///     加载并返回配置树根节点�?    ///
    /// </summary>
    /// <returns>配置树的根节点�?/returns>
    ConfigNode load();
}