namespace Nyar.VM.TextVM;

/// <summary>
/// 替换模板 token。
/// </summary>
public readonly struct ReplaceToken
{
    /// <summary>
    /// token 类型：Literal（字面量字节）或 GroupRef（捕获组引用 $0-$9）。
    /// </summary>
    public ReplaceTokenKind Kind { get; }

    /// <summary>
    /// 字面量字节（仅 Kind == Literal 时有效）。
    /// </summary>
    public Byte[] LiteralBytes { get; }

    /// <summary>
    /// 捕获组编号（仅 Kind == GroupRef 时有效）。
    /// </summary>
    public Int32 GroupId { get; }

    private ReplaceToken(ReplaceTokenKind kind, Byte[]? literalBytes = null, Int32 groupId = 0)
    {
        Kind = kind;
        LiteralBytes = literalBytes ?? [];
        GroupId = groupId;
    }

    /// <summary>
    /// 创建字面量 token。
    /// </summary>
    public static ReplaceToken CreateLiteral(Byte[] bytes) => new(ReplaceTokenKind.Literal, literalBytes: bytes);

    /// <summary>
    /// 创建捕获组引用 token。
    /// </summary>
    public static ReplaceToken CreateGroupRef(Int32 groupId) => new(ReplaceTokenKind.GroupRef, groupId: groupId);
}

/// <summary>
/// 替换 token 类型。
/// </summary>
public enum ReplaceTokenKind
{
    /// <summary>
    /// 字面量字节。
    /// </summary>
    Literal,

    /// <summary>
    /// 捕获组引用 $0-$9。
    /// </summary>
    GroupRef,
}

/// <summary>
/// 替换模板解析器。将 "$1/$2" 等模板字符串解析为 token 序列。
/// </summary>
public static class ReplaceTemplate
{
    /// <summary>
    /// 解析替换模板字符串。
    /// 支持：$0（全匹配）、$1-$9（捕获组）、$$（转义为 $ 字面量）。
    /// </summary>
    public static ReplaceToken[] Parse(String template)
    {
        List<ReplaceToken> tokens = [];
        List<Byte> currentLiteral = [];

        for (Int32 i = 0; i < template.Length; i++)
        {
            Char c = template[i];

            if (c == '$')
            {
                if (i + 1 < template.Length)
                {
                    Char next = template[i + 1];

                    if (next == '$')
                    {
                        // $$ 转义为 $ 字面量
                        currentLiteral.Add((Byte)'$');
                        i++;
                        continue;
                    }

                    if (next >= '0' && next <= '9')
                    {
                        // $0-$9 捕获组引用
                        FlushLiteral(tokens, currentLiteral);
                        tokens.Add(ReplaceToken.CreateGroupRef(next - '0'));
                        i++;
                        continue;
                    }
                }

                // 末尾单独的 $ 视为字面量
                currentLiteral.Add((Byte)'$');
            }
            else
            {
                // 将字符转换为 UTF-8 字节序列
                foreach (Byte b in System.Text.Encoding.UTF8.GetBytes([c]))
                {
                    currentLiteral.Add(b);
                }
            }
        }

        FlushLiteral(tokens, currentLiteral);
        return [.. tokens];
    }

    private static void FlushLiteral(List<ReplaceToken> tokens, List<Byte> currentLiteral)
    {
        if (currentLiteral.Count > 0)
        {
            tokens.Add(ReplaceToken.CreateLiteral([.. currentLiteral]));
            currentLiteral.Clear();
        }
    }

    /// <summary>
    /// 将替换 token 序列与匹配结果合并为输出字节序列。
    /// </summary>
    public static Byte[] Apply(ReplaceToken[] tokens, ReadOnlySpan<Byte> input, Match match, Int32[]? captures)
    {
        List<Byte> result = [];

        foreach (ReplaceToken token in tokens)
        {
            switch (token.Kind)
            {
                case ReplaceTokenKind.Literal:
                    result.AddRange(token.LiteralBytes);
                    break;

                case ReplaceTokenKind.GroupRef:
                    if (token.GroupId == 0)
                    {
                        // $0 = 整个匹配
                        result.AddRange([.. input.Slice(match.Start, match.Length)]);
                    }
                    else if (captures is not null && token.GroupId * 2 + 1 < captures.Length)
                    {
                        Int32 capStart = captures[token.GroupId * 2];
                        Int32 capEnd = captures[token.GroupId * 2 + 1];
                        if (capStart >= 0 && capEnd >= capStart)
                        {
                            result.AddRange([.. input.Slice(capStart, capEnd - capStart)]);
                        }
                    }
                    break;
            }
        }

        return [.. result];
    }
}
