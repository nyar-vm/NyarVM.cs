namespace Std.Data.Text.Valkyrie.AST.Pattern;

/// <summary>
///     类型模式 —— 按类型匹配并可选绑定变量，用于 <c>match</c> 语句中
/// </summary>
/// <para>示例：</para>
/// <code>
/// match value {
///     case 0: print("整数 {value}");
///     type utf8: print("字符串");
///     when n == 0: print("浮点 {value}");
///     else: 
/// }
/// </code>
public abstract record PatternNode : ValkyrieNode
{
}