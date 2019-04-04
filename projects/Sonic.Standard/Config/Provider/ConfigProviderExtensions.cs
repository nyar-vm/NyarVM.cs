namespace Std.Config.Provider;

/// <summary>
///     IConfigProvider 的扩展方法�?///
/// </summary>
public static class ConfigProviderExtensions
{
    /// <summary>
    ///     将配置源包装为可选源，加载失败时返回空节点而非抛出异常�?    ///
    /// </summary>
    /// <param name="provider">
    ///     原始配置源�?/param>
    ///     <returns>包装后的可选配置源�?/returns>
    public static IConfigProvider optional(this IConfigProvider provider)
    {
        return new OptionalConfigProvider(provider);
    }
}