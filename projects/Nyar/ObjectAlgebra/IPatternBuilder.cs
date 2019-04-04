namespace Nyar.ObjectAlgebra;

/// <summary>
///     模式构建器接口，用于在规则 DSL 中构建可匹配的模式。
///     每个方言应生成对应的 PatternBuilder 实现。
/// </summary>
/// <typeparam name="P">模式表示类型。</typeparam>
public interface IPatternBuilder<P>
{
    /// <summary>
    ///     匹配任意节点，绑定到指定名称。
    ///     同名绑定在整个模式中共享。
    /// </summary>
    /// <param name="name">绑定名称。</param>
    /// <returns>模式表达式。</returns>
    P Any(string name);

    /// <summary>
    ///     兼容旧的小写成员名。
    /// </summary>
    P any(string name)
    {
        return Any(name);
    }
}
