namespace Nyar.ObjectAlgebra;

/// <summary>
///     所有 OA 方言工厂的标记接口。
///     每个方言的泛型工厂接口（如 ICoreAlgebra
///     <E>
///         , IStandardAlgebra
///         <E>
///             ）都应继承此接口。
///             E 为表示类型参数，不同工厂实现绑定不同的 E。
/// </summary>
/// <typeparam name="E">表示类型参数</typeparam>
public interface IAlgebra<E>
{
}