namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     表达式语句，以分号结尾的独立表达式（表示执行并丢弃结果）
/// </summary>
/// <para>示例：</para>
/// <code>
/// print("hello");          // Expression = TermCallExpression
/// x += 1;                  // Expression = AssignmentExpr
/// 42;                      // Expression = LiteralExpr（虽然无意义但是合法）
/// </code>
public abstract record TermNode : ValkyrieNode
{
}