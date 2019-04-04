using Std.Data.Text.Syntax;

namespace Oak.Wat;

/// <summary>
///     WAT Token
/// </summary>
/// <param name="Type">Token 类型。</param>
/// <param name="Value">Token 值。</param>
/// <param name="Line">行号。</param>
/// <param name="Column">列号。</param>
public sealed record WatToken(WatTokenType Type, string Value, int Line, int Column)
{
    /// <summary>
    ///     转换为源码位置
    /// </summary>
    public TextSpan ToSourceSpan()
    {
        return default;
    }
}