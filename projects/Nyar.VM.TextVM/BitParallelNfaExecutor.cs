namespace Nyar.VM.TextVM;

/// <summary>
/// 基于 Glushkov 位并行 NFA 的执行器。使用 64 位掩码跟踪活动状态集，
/// 适用于状态数不超过 64 的模式。采用 O(n) 位运算进行匹配。
/// </summary>
public sealed class BitParallelNfaExecutor : CompiledUnit
{
    /// <summary>
    /// 接受状态掩码。位 i 为 1 表示状态 i 是接受状态。
    /// </summary>
    private readonly UInt64 _acceptingMask;

    /// <summary>
    /// 每个字符的 Follow 掩码，按字符索引到可到达的状态位集合。
    /// </summary>
    private readonly Dictionary<UInt16, UInt64> _charMasks;

    /// <summary>
    /// 文本编码类型。
    /// </summary>
    private readonly TextEncoding _encoding;

    /// <summary>
    /// 使用给定的序列化 NFA 表字节数据和编码创建位并行 NFA 执行器。
    /// </summary>
    /// <param name="serializedNfaTable">序列化的 NFA 表字节数据。</param>
    /// <param name="encoding">文本编码类型。</param>
    /// <exception cref="ArgumentNullException"><paramref name="serializedNfaTable"/> 为 null。</exception>
    public BitParallelNfaExecutor(Byte[] serializedNfaTable, TextEncoding encoding)
    {
        if (serializedNfaTable is null)
        {
            throw new ArgumentNullException(nameof(serializedNfaTable));
        }

        ReadOnlySpan<Byte> data = serializedNfaTable.AsSpan();
        Int32 offset = 0;

        _acceptingMask = BitConverter.ToUInt64(data.Slice(offset, 8));
        offset += 8;

        Int32 entryCount = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;

        _charMasks = new Dictionary<UInt16, UInt64>(entryCount);

        for (Int32 i = 0; i < entryCount; i++)
        {
            UInt16 ch = BitConverter.ToUInt16(data.Slice(offset, 2));
            offset += 2;

            UInt64 mask = BitConverter.ToUInt64(data.Slice(offset, 8));
            offset += 8;

            _charMasks[ch] = mask;
        }

        _encoding = encoding;
    }

    /// <summary>
    /// 在输入切片的指定起始位置运行位并行 NFA，返回最长匹配结果。
    /// </summary>
    /// <param name="input">输入字节切片。</param>
    /// <param name="startPos">搜索起始字节偏移。</param>
    /// <returns>最长匹配区间，未找到时返回 null。</returns>
    private Match? TryMatchAt(ReadOnlySpan<Byte> input, Int32 startPos)
    {
        ReadOnlySpan<Byte> slice = input.Slice(startPos);
        CodePointIter iter = new CodePointIter(slice, _encoding);
        UInt64 active = 1; // 初始状态为位 0（起始状态）
        Int32 consumedBytes = 0;
        Match? bestMatch = null;

        while (iter.TryNext(out Int32 byteLen, out Char ch))
        {
            UInt16 chVal = (UInt16)ch;

            if (_charMasks.TryGetValue(chVal, out UInt64 charMask))
            {
                active = ((active << 1) | 1) & charMask;
            }
            else
            {
                active = 0;
            }

            consumedBytes += byteLen;

            if ((active & _acceptingMask) != 0)
            {
                bestMatch = new Match(startPos, startPos + consumedBytes);
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// 判断输入中是否存在 NFA 匹配。
    /// </summary>
    public override Boolean IsMatch(ReadOnlySpan<Byte> input)
    {
        UInt64 active = 1;

        CodePointIter iter = new CodePointIter(input, _encoding);

        while (iter.TryNext(out Int32 byteLen, out Char ch))
        {
            UInt16 chVal = (UInt16)ch;

            if (_charMasks.TryGetValue(chVal, out UInt64 charMask))
            {
                active = ((active << 1) | 1) & charMask;
            }
            else
            {
                active = 0;
            }

            if ((active & _acceptingMask) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 查找输入中第一个 NFA 匹配区间。
    /// </summary>
    public override Match? FindFirst(ReadOnlySpan<Byte> input)
    {
        CodePointIter outerIter = new CodePointIter(input, _encoding);
        Int32 startPos = 0;

        while (startPos < input.Length)
        {
            Match? match = TryMatchAt(input, startPos);

            if (match is not null)
            {
                return match;
            }

            if (!outerIter.TryNext(out Int32 byteLen, out _))
            {
                break;
            }

            startPos += byteLen;
        }

        return null;
    }

    /// <summary>
    /// 查找输入中所有 NFA 匹配区间（非重叠，左到右最长匹配）。
    /// </summary>
    public override IEnumerable<Match> FindAll(ReadOnlySpan<Byte> input)
    {
        return FindAllInternal(input);
    }

    /// <summary>
    /// 执行替换，将输入中所有匹配替换为替换字节序列。
    /// </summary>
    public override Byte[] Replace(ReadOnlySpan<Byte> input, ReadOnlySpan<Byte> replacement)
    {
        List<Match> matches = FindAllInternal(input);

        if (matches.Count == 0)
        {
            return [.. input];
        }

        Int32 newSize = input.Length;
        Int32 replLen = replacement.Length;

        foreach (Match match in matches)
        {
            newSize += replLen - match.Length;
        }

        Byte[] result = new Byte[newSize];
        Int32 destPos = 0;
        Int32 srcPos = 0;

        foreach (Match match in matches)
        {
            Int32 copyLen = match.Start - srcPos;
            input.Slice(srcPos, copyLen).CopyTo(result.AsSpan(destPos));
            destPos += copyLen;

            replacement.CopyTo(result.AsSpan(destPos));
            destPos += replLen;

            srcPos = match.End;
        }

        input.Slice(srcPos).CopyTo(result.AsSpan(destPos));

        return result;
    }

    /// <summary>
    /// 内部查找所有匹配区间，遍历每个可能的起始位置。
    /// </summary>
    private List<Match> FindAllInternal(ReadOnlySpan<Byte> input)
    {
        List<Match> matches = [];
        CodePointIter outerIter = new CodePointIter(input, _encoding);
        Int32 startPos = 0;

        while (startPos < input.Length)
        {
            Match? match = TryMatchAt(input, startPos);

            if (match is not null)
            {
                matches.Add(match.Value);
                startPos = match.Value.End;
            }
            else
            {
                if (!outerIter.TryNext(out Int32 byteLen, out _))
                {
                    break;
                }

                startPos += byteLen;
            }
        }

        return matches;
    }
}
