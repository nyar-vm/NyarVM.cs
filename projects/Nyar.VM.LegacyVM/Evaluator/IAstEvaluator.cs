namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     AST 求值器接口。
///     遍历 AST 节点并调用 CoreEvaluator 求值。
/// </summary>
/// <typeparam name="TAstNode">AST 节点类型</typeparam>
public interface IAstEvaluator<TAstNode>
{
    /// <summary>
    ///     求值 AST 根节点
    /// </summary>
    /// <param name="ast">AST 根节点</param>
    /// <returns>求值结果</returns>
    object evaluate(TAstNode ast);
}