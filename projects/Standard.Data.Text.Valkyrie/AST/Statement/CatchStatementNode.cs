using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Catch 错误处理语句，捕获表达式可能抛出的错误并按模式匹配处理
/// </summary>
/// <para>示例：</para>
/// <code>
/// catch parse_json(data)
///     case JsonError { msg }: log(msg)
///     else: null
/// </code>
public sealed record CatchStatementNode : ValkyrieNode
{
    /// <summary>
    ///     被监视的表达式
    /// </summary>
    public TermNode expression { get; init; }

    /// <summary>
    ///     错误处理分支列表
    /// </summary>
    public IReadOnlyList<ArmNode> arms { get; init; } = [];
}