using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Raise 语句，抛出效应操作
/// </summary>
/// <para>示例：</para>
/// <code>
/// raise Get;
/// raise Put { new_value: 42 };
/// raise Log { msg: "negative state" };
/// </code>
public sealed record RaiseStatement : ValkyrieNode
{
    /// <summary>
    ///     被抛出的效应操作表达式
    /// </summary>
    public TermNode value { get; init; }
}