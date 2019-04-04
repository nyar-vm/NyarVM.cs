using System;

namespace Core.Config;

/// <summary>
///     配置监视器接口，提供配置键变更的监听能力
/// </summary>
public interface IConfigWatcher : IDisposable
{
    /// <summary>
    ///     监视指定配置键的变更
    /// </summary>
    /// <param name="key">要监视的配置键</param>
    /// <param name="onChange">配置值变更时的回调函数</param>
    void watch(string key, Action<string?> onChange);
}