using Microsoft.Extensions.Options;

namespace Std.Config;

/// <summary>
///     基于配置热加载监视器�?<see cref="IOptionsMonitor{T}" /> 实现�?/// 当配置重载时自动通知所有监听器�?///
/// </summary>
/// <typeparam name="T">配置类型�?/typeparam>
internal sealed class OptionsMonitorFromWatcher<T> : IOptionsMonitor<T>
    where T : class, IConfigurable
{
    #region 字段

    /// <summary>
    ///     配置热加载监视器�?    ///
    /// </summary>
    private readonly IConfigWatcher<T> _watcher;

    #endregion

    #region 构造函�?

    /// <summary>
    ///     使用配置监视器初始化 <see cref="OptionsMonitorFromWatcher{T}" /> 的新实例�?    ///
    /// </summary>
    /// <param name="watcher">配置热加载监视器�?/param>
    public OptionsMonitorFromWatcher(IConfigWatcher<T> watcher)
    {
        _watcher = watcher;
        _watcher.OnReloaded += OnConfigReloaded;
    }

    #endregion

    #region 属�?

    /// <summary>
    ///     获取当前配置值�?    ///
    /// </summary>
    public T CurrentValue => _watcher.current;

    #endregion

    #region 私有方法

    /// <summary>
    ///     配置重载事件处理�?    ///
    /// </summary>
    /// <param name="config">重载后的配置实例�?/param>
    private void OnConfigReloaded(T config)
    {
        // IOptionsMonitor 的变更通知通过 OnChange 注册的监听器传播
    }

    #endregion

    #region 嵌套类型

    /// <summary>
    ///     变更监听器的可释放包装，用于取消注册监听器�?    ///
    /// </summary>
    /// <typeparam name="TConfig">配置类型�?/typeparam>
    private sealed class ChangeListenerDisposable<TConfig> : IDisposable
        where TConfig : class, IConfigurable
    {
        #region 构造函�?

        /// <summary>
        ///     使用监视器和处理器初始化 <see cref="ChangeListenerDisposable{TConfig}" /> 的新实例�?        ///
        /// </summary>
        /// <param name="watcher">
        ///     配置热加载监视器�?/param>
        ///     <param name="handler">已注册的事件处理器�?/param>
        public ChangeListenerDisposable(IConfigWatcher<TConfig> watcher, Action<TConfig> handler)
        {
            _watcher = watcher;
            _handler = handler;
        }

        #endregion

        #region 公开方法

        /// <summary>
        ///     取消注册事件处理器�?        ///
        /// </summary>
        public void Dispose()
        {
            _watcher.OnReloaded -= _handler;
        }

        #endregion

        #region 字段

        /// <summary>
        ///     配置热加载监视器�?        ///
        /// </summary>
        private readonly IConfigWatcher<TConfig> _watcher;

        /// <summary>
        ///     已注册的事件处理器�?        ///
        /// </summary>
        private readonly Action<TConfig> _handler;

        #endregion
    }

    #endregion

    #region 公开方法

    /// <summary>
    ///     获取指定名称的配置值�?    ///
    /// </summary>
    /// <param name="name">
    ///     配置名称，当前实现忽略此参数�?/param>
    ///     <returns>当前配置实例�?/returns>
    public T Get(string? name)
    {
        return _watcher.current;
    }

    /// <summary>
    ///     注册变更监听器，配置重载时触发�?    ///
    /// </summary>
    /// <param name="listener">
    ///     变更监听回调�?/param>
    ///     <returns>可释放对象，调用 <see cref="IDisposable.Dispose" /> 取消监听�?/returns>
    public IDisposable? OnChange(Action<T, string?> listener)
    {
        Action<T> handler = config => listener(config, null);
        _watcher.OnReloaded += handler;
        return new ChangeListenerDisposable<T>(_watcher, handler);
    }

    #endregion
}