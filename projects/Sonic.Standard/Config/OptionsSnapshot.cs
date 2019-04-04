using Microsoft.Extensions.Options;

namespace Std.Config;

/// <summary>
///     基于配置监视器的 <see cref="IOptionsSnapshot{T}" /> 实现�?///
/// </summary>
/// <typeparam name="T">配置类型�?/typeparam>
internal sealed class OptionsSnapshot<T> : IOptionsSnapshot<T>
    where T : class, IConfigurable
{
    #region 字段

    /// <summary>
    ///     配置监视器�?    ///
    /// </summary>
    private readonly IOptionsMonitor<T> _monitor;

    #endregion

    #region 构造函�?

    /// <summary>
    ///     使用配置监视器初始化 <see cref="OptionsSnapshot{T}" /> 的新实例�?    ///
    /// </summary>
    /// <param name="monitor">配置监视器�?/param>
    public OptionsSnapshot(IOptionsMonitor<T> monitor)
    {
        _monitor = monitor;
    }

    #endregion

    #region 属�?

    /// <summary>
    ///     获取当前配置值�?    ///
    /// </summary>
    public T Value => _monitor.CurrentValue;

    #endregion

    #region 公开方法

    /// <summary>
    ///     获取指定名称的配置值�?    ///
    /// </summary>
    /// <param name="name">
    ///     配置名称�?/param>
    ///     <returns>配置实例�?/returns>
    public T Get(string? name)
    {
        return _monitor.Get(name);
    }

    #endregion
}