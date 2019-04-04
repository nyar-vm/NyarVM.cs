namespace Hermes.Config;

/// <summary>
///     强类型 Config Snapshot — 每次访问 .Value 获取当前最新值
/// </summary>
/// <typeparam name="T">Config 类型</typeparam>
public interface IConfigSnapshot<T>
    where T : class
{
    /// <summary>
    ///     当前配置值
    /// </summary>
    T Value { get; }
}