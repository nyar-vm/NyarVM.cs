using Std.Data.Text.Valkyrie.AST.Statement;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR Catch 分支，表示 <c>catch { case Pattern: body }</c> 中的单个效应捕获分支。
/// </summary>
public sealed record HirCatchArm
{
    /// <summary>
    ///     匹配模式 AST 节点。
    /// </summary>
    public AstNode pattern { get; init; } = new ArmCaseNode();

    /// <summary>
    ///     分支体代码块。
    /// </summary>
    public FunctionBody body { get; init; } = new();

    /// <summary>
    ///     是否包含 <c>resume</c> 调用，表示该分支为可恢复效应处理分支。
    /// </summary>
    public bool has_resume { get; init; }
}