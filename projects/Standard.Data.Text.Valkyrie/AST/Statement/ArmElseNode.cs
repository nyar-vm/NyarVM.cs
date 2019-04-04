namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Catch 分支，匹配特定错误类型的处理臂
/// </summary>
/// <para>示例：</para>
/// <code>
/// catch risky_operation()
///     case Some(_): ...
///     else: ...
/// </code>
public sealed record ArmElseNode : ArmNode
{
}