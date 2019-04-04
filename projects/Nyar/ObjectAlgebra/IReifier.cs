namespace Nyar.ObjectAlgebra;

/// <summary>
///     Reifier 接口，将 OA algebra 构造映射为 EGraph 中的 Id。
///     每个方言应有一个对应的 Reifier 实现，由 Source Generator 生成。
/// </summary>
/// <typeparam name="E">表示类型参数（通常为 Id）</typeparam>
public interface IReifier<E> : IAlgebra<E>
{
    /// <summary>
    ///     获取在此 Reifier 中已注册的符号表。
    ///     符号表将 dialect 内每个 operator name 映射到其 `IOperatorDescriptor`。
    /// </summary>
    IReadOnlyDictionary<string, IOperatorDescriptor> Symbols { get; }

    /// <summary>
    ///     兼容旧的小写成员名。
    /// </summary>
    IReadOnlyDictionary<string, IOperatorDescriptor> symbols => Symbols;
}
