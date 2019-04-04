namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Break 语句，从当前循环中跳出。
/// </summary>
/// <para>示例：</para>
/// <code>
/// break;
/// </code>
public sealed record BreakStatement : ValkyrieNode;
