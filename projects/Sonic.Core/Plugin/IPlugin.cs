namespace Core.Plugin;

/// <summary>
///     IPlugin 接口
/// </summary>
public interface IPlugin
{
    /// <summary>
    ///     插件名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     插件版本
    /// </summary>
    string version { get; }

    /// <summary>
    ///     初始化插件
    /// </summary>
    void initialize(IPluginHost host);
}