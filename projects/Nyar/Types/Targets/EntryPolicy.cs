namespace Nyar.Types.Targets;

/// <summary>
///     入口策略，定义逻辑入口到物理入口的映射规则。
/// </summary>
public sealed record EntryPolicy
{
    /// <summary>
    ///     默认入口名称
    /// </summary>
    public string default_entry { get; init; } = "main";

    /// <summary>
    ///     入口包装策略
    /// </summary>
    public WrapStrategy wrap_strategy { get; init; } = WrapStrategy.direct;

    /// <summary>
    ///     是否生成入口包装函数
    /// </summary>
    public bool generate_wrapper { get; init; }
}