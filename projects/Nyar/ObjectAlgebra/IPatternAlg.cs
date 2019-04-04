namespace Nyar.ObjectAlgebra;

/// <summary>
///     模式代数接口，用于在规则系统中描述可匹配的模式。
///     每个方言应生成对应的 PatternAlg 接口，由 Source Generator 生成。
/// </summary>
/// <typeparam name="P">模式表示类型。</typeparam>
public interface IPatternAlg<P> : IAlgebra<P>
{
    /// <summary>
    ///     匹配任意节点，绑定到指定名称
    /// </summary>
    /// <param name="name">绑定名称。</param>
    /// <returns>模式表达式。</returns>
    P any(string name);
}