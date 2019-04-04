namespace Oak.Wat;

/// <summary>
///     WAT Token 流导航器，提供对 Token 序列的读取和模式匹配操作
/// </summary>
public sealed class WatTokenStream
{
    private readonly IReadOnlyList<WatToken> _tokens;


    /// <summary>
    ///     使用 Token 列表初始化导航器
    /// </summary>
    /// <param name="tokens">Token 序列。</param>
    public WatTokenStream(IReadOnlyList<WatToken> tokens)
    {
        _tokens = tokens;
        Position = 0;
    }


    /// <summary>
    ///     当前 Token
    /// </summary>
    public WatToken Current => Position < _tokens.Count ? _tokens[Position] : _tokens[_tokens.Count - 1];


    /// <summary>
    ///     当前位置
    /// </summary>
    public int Position { get; private set; }


    /// <summary>
    ///     前进到下一个 Token 并返回前进前的 Token
    /// </summary>
    /// <returns>前进前的 Token。</returns>
    public WatToken Advance()
    {
        var token = Current;
        if (Position < _tokens.Count) Position++;

        return token;
    }


    /// <summary>
    ///     向前看 offset 个位置，不移动当前位置
    /// </summary>
    /// <param name="offset">偏移量，默认为 1。</param>
    /// <returns>偏移位置的 Token。</returns>
    public WatToken Peek(int offset = 1)
    {
        var index = Position + offset;
        return index < _tokens.Count ? _tokens[index] : _tokens[_tokens.Count - 1];
    }


    /// <summary>
    ///     移动到指定位置
    /// </summary>
    /// <param name="position">目标位置。</param>
    public void Seek(int position)
    {
        Position = position;
    }


    /// <summary>
    ///     重置到起始位置
    /// </summary>
    public void Reset()
    {
        Position = 0;
    }


    /// <summary>
    ///     检查当前 Token 是否匹配指定类型和值
    /// </summary>
    /// <param name="type">期望的 Token 类型。</param>
    /// <param name="value">期望的 Token 值，为 null 时仅匹配类型。</param>
    /// <returns>是否匹配。</returns>
    public bool Check(WatTokenType type, string? value = null)
    {
        var token = Current;
        if (token.Type != type) return false;

        return value is null || token.Value == value;
    }


    /// <summary>
    ///     如果当前 Token 匹配则前进，否则不移动
    /// </summary>
    /// <param name="type">期望的 Token 类型。</param>
    /// <param name="value">期望的 Token 值，为 null 时仅匹配类型。</param>
    /// <returns>是否匹配并已前进。</returns>
    public bool Match(WatTokenType type, string? value = null)
    {
        if (!Check(type, value)) return false;

        Advance();
        return true;
    }


    /// <summary>
    ///     期望当前 Token 匹配指定类型和值，不匹配则抛出 <see cref="WatParseException" />
    /// </summary>
    /// <param name="type">期望的 Token 类型。</param>
    /// <param name="value">期望的 Token 值，为 null 时仅匹配类型。</param>
    /// <returns>匹配的 Token。</returns>
    /// <exception cref="WatParseException">当前 Token 不匹配时抛出</exception>
    public WatToken Expect(WatTokenType type, string? value = null)
    {
        var token = Current;
        if (token.Type != type)
            throw new WatParseException(
                $"期望 Token 类型为 {type}，实际为 {token.Type}",
                token.Line,
                token.Column);

        if (value is not null && token.Value != value)
            throw new WatParseException(
                $"期望 Token 值为 '{value}'，实际为 '{token.Value}'",
                token.Line,
                token.Column);

        Advance();
        return token;
    }


    /// <summary>
    ///     是否已到达末尾
    /// </summary>
    /// <returns>当前位置是否在末尾 Token。</returns>
    public bool IsAtEnd()
    {
        return Current.Type == WatTokenType.Eof;
    }
}