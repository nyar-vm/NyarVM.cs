namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Continue 语句，跳过当前循环剩余部分并进入下一轮。
/// </summary>
/// <para>示例：</para>
/// <code>
/// continue;
/// </code>
public sealed record ContinueStatement : ValkyrieNode;
