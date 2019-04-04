using Std.Data.Text.Syntax;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     Jasmin Token
/// </summary>
/// <param name="type">Token 类型。</param>
/// <param name="value">Token 值。</param>
/// <param name="line">行号。</param>
/// <param name="column">列号。</param>
public sealed record JmToken(JmTokenType type, string value, int line, int column)
{
    /// <summary>
    ///     转换为源码位置
    /// </summary>
    public TextSpan to_source_span()
    {
        return default;
    }
}