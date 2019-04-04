namespace Nyar.Language.Valkyrie.Compiler.Lir;

/// <summary>
///     <see cref="LirBuilder" /> 的 partial：模式匹配降级相关方法。
///     模式匹配节点已从 AlgebraNode 中移除，match 语句/表达式现在直接通过
///     <c>Choice</c> 与比较/逻辑核心节点在 MIR 层完成降级，不再需要此文件中的方法。
/// </summary>
public sealed partial class LirBuilder
{
}
