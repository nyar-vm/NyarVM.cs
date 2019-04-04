namespace Std.Data.Text.Valkyrie.AST.Pattern;

/// <summary>
///     布尔常量模式 —— 匹配 `true/false`
/// </summary>
/// <para>示例：</para>
/// <code>
/// case true;
/// </code>
public sealed record PatternLiteralBooleanNode : PatternNode
{
    /// <summary>
    ///     布尔字面量的值。
    /// </summary>
    public bool value { get; init; }
}