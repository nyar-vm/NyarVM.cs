namespace Std.Config;

/// <summary>
///     配置热加载监视器接口，监控配置源变化并自动重建配置�?///
/// </summary>
/// <typeparam name="T">配置类型，必须实�?<see cref="IConfigurable" />�?/typeparam>
public interface IConfigWatcher<out T> : IDisposable where T : IConfigurable
{
    /// <summary>
    ///     获取当前的配置实例�?    ///
    /// </summary>
    T current { get; }

    /// <summary>
    ///     配置重载完成时触发的事件�?    ///
    /// </summary>
    event Action<T>? OnReloaded;
}