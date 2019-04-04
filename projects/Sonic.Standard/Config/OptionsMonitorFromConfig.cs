using Microsoft.Extensions.Options;

namespace Std.Config;

/// <summary>
///     基于静态配置实例的 <see cref="IOptionsMonitor{T}" /> 实现�?///
/// </summary>
/// <typeparam name="T">配置类型�?/typeparam>
internal sealed class OptionsMonitorFromConfig<T> : IOptionsMonitor<T>
    where T : class, IConfigurable
{
    #region 字段

    /// <summary>
    ///     静态配置实例�?    ///
    /// </summary>
    private readonly T _config;

    #endregion

    #region 构造函�?

    /// <summary>
    ///     使用静态配置实例初始化 <see cref="OptionsMonitorFromConfig{T}" /> 的新实例�?    ///
    /// </summary>
    /// <param name="config">静态配置实例�?/param>
    public OptionsMonitorFromConfig(T config)
    {
        _config = config;
    }

    #endregion

    #region 属�?

    /// <summary>
    ///     获取当前配置值�?    ///
    /// </summary>
    public T CurrentValue => _config;

    #endregion

    #region 公开方法

    /// <summary>
    ///     获取指定名称的配置值�?    ///
    /// </summary>
    /// <param name="name">
    ///     配置名称，静态配置忽略此参数�?/param>
    ///     <returns>配置实例�?/returns>
    public T Get(string? name)
    {
        return _config;
    }

    /// <summary>
    ///     注册变更监听器。静态配置不会触发变更，始终返回 null�?    ///
    /// </summary>
    /// <param name="listener">
    ///     变更监听回调�?/param>
    ///     <returns>始终返回 null，因为静态配置不会变更�?/returns>
    public IDisposable? OnChange(Action<T, string?> listener)
    {
        return null;
    }

    #endregion
}