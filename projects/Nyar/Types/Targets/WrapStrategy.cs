namespace Nyar.Types.Targets;

/// <summary>
///     入口包装策略枚举，定义逻辑入口到物理入口的映射方式
/// </summary>
public enum WrapStrategy
{
    /// <summary>
    ///     直接调用入口函数，不生成包装
    /// </summary>
    direct,

    /// <summary>
    ///     生成包装函数（如 CLR/JVM 的标准入口包装）
    /// </summary>
    wrapper,

    /// <summary>
    ///     由宿主环境托管入口（如浏览器的 WASM 加载机制）
    /// </summary>
    hosted
}