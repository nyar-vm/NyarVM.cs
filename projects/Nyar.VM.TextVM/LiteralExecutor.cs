namespace Nyar.VM.TextVM;

/// <summary>
/// 字面量执行器。使用 TwoWayMatcher 进行快速字面量搜索。
/// 适用于纯字面量模式的最优路径。
/// </summary>
public sealed class LiteralExecutor : CompiledUnit
{
    private readonly Byte[] _pattern;

    private readonly TextEncoding _encoding;

    /// <summary>
    /// 使用给定的模式字节序列和编码创建字面量执行器。
    /// </summary>
    /// <param name="pattern">要匹配的字面量模式字节序列。</param>
    /// <param name="encoding">文本编码类型。</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> 为 null。</exception>
    public LiteralExecutor(Byte[] pattern, TextEncoding encoding)
    {
        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        _pattern = pattern;
        _encoding = encoding;
    }

    /// <summary>
    /// 判断输入中是否存在与字面量模式的匹配。
    /// </summary>
    public override Boolean IsMatch(ReadOnlySpan<Byte> input)
    {
        return TwoWayMatcher.IndexOf(input, _pattern.AsSpan()) >= 0;
    }

    /// <summary>
    /// 查找输入中第一个与字面量模式匹配的区间。
    /// </summary>
    public override Match? FindFirst(ReadOnlySpan<Byte> input)
    {
        Int32 pos = TwoWayMatcher.IndexOf(input, _pattern.AsSpan());

        if (pos < 0)
        {
            return null;
        }

        return new Match(pos, pos + _pattern.Length);
    }

    /// <summary>
    /// 查找输入中所有与字面量模式匹配的区间（非重叠，左到右）。
    /// </summary>
    public override IEnumerable<Match> FindAll(ReadOnlySpan<Byte> input)
    {
        return FindAllInternal(input);
    }

    /// <summary>
    /// 执行替换，将输入中所有匹配的字面量替换为替换字节序列。
    /// </summary>
    public override Byte[] Replace(ReadOnlySpan<Byte> input, ReadOnlySpan<Byte> replacement)
    {
        List<Match> matches = FindAllInternal(input);

        if (matches.Count == 0)
        {
            return [.. input];
        }

        // 计算替换后的总长度
        Int32 newSize = input.Length;
        Int32 patternLen = _pattern.Length;
        Int32 replLen = replacement.Length;

        foreach (Match match in matches)
        {
            newSize += replLen - patternLen;
        }

        // 构建结果
        Byte[] result = new Byte[newSize];
        Int32 destPos = 0;
        Int32 srcPos = 0;

        foreach (Match match in matches)
        {
            // 复制匹配位置之前的字节
            Int32 copyLen = match.Start - srcPos;
            input.Slice(srcPos, copyLen).CopyTo(result.AsSpan(destPos));
            destPos += copyLen;

            // 写入替换字节
            replacement.CopyTo(result.AsSpan(destPos));
            destPos += replLen;

            srcPos = match.End;
        }

        // 复制剩余字节
        input.Slice(srcPos).CopyTo(result.AsSpan(destPos));

        return result;
    }

    /// <summary>
    /// 内部查找所有匹配区间，使用 TwoWayMatcher 遍历。
    /// </summary>
    private List<Match> FindAllInternal(ReadOnlySpan<Byte> input)
    {
        List<Match> matches = [];
        Int32 searchStart = 0;

        while (searchStart < input.Length)
        {
            ReadOnlySpan<Byte> slice = input.Slice(searchStart);
            Int32 pos = TwoWayMatcher.IndexOf(slice, _pattern.AsSpan());

            if (pos < 0)
            {
                break;
            }

            matches.Add(new Match(searchStart + pos, searchStart + pos + _pattern.Length));
            searchStart += pos + _pattern.Length;
        }

        return matches;
    }
}
