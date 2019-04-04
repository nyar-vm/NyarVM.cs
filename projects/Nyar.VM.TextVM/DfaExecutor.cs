namespace Nyar.VM.TextVM;

/// <summary>
/// DFA 执行器。基于确定性有限自动机进行模式匹配。
/// 序列化 DFA 表格式：4 字节状态数 + 每个状态（1 字节接受标志 + 4 字节默认转移 + 4 字节转移数 + N *（2 字节字符 + 4 字节下一状态））。
/// 支持两种输入：完整 .tvm 文件（带 32 字节 TvmHeader）和裸 DFA 表数据。
/// 通过检查前 4 字节是否为 "TVM\0" 来自动识别。
/// </summary>
public sealed class DfaExecutor : CompiledUnit
{
    /// <summary>
    /// 每个状态是否为接受状态。
    /// </summary>
    private readonly Boolean[] _accepting;

    /// <summary>
    /// 每个状态的默认转移目标，-1 表示无默认转移。
    /// </summary>
    private readonly Int32[] _defaultTransitions;

    /// <summary>
    /// 每个状态的转移表，使用锯齿数组 [state][char] = nextState，-1 表示无转移。
    /// 对于有默认转移的状态，先填充默认值再覆盖显式条目。
    /// </summary>
    private readonly Int32[][] _transitions;

    /// <summary>
    /// 文本编码类型。
    /// </summary>
    private readonly TextEncoding _encoding;

    /// <summary>
    /// 使用给定的序列化 DFA 表字节数据和编码创建 DFA 执行器。
    /// 自动检测输入是否为完整 .tvm 文件（带 TvmHeader），并相应处理。
    /// 序列化格式：stateCount(4B) + 每个状态(accepting(1B) + defaultTransition(4B) + transitionCount(4B) + entries)。
    /// </summary>
    public DfaExecutor(Byte[] serializedDfaTable, TextEncoding encoding)
    {
        if (serializedDfaTable is null)
        {
            throw new ArgumentNullException(nameof(serializedDfaTable));
        }

        ReadOnlySpan<Byte> data = serializedDfaTable.AsSpan();
        Int32 offset = 0;

        // 检测是否为完整 .tvm 文件，如果是则跳过 32 字节 Header
        if (data.Length >= TvmHeader.HeaderSize &&
            data[0] == (Byte)'T' && data[1] == (Byte)'V' &&
            data[2] == (Byte)'M' && data[3] == (Byte)0)
        {
            offset = TvmHeader.HeaderSize;
        }

        // 读取状态数
        Int32 stateCount = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;

        _accepting = new Boolean[stateCount];
        _defaultTransitions = new Int32[stateCount];
        _transitions = new Int32[stateCount][];

        for (Int32 s = 0; s < stateCount; s++)
        {
            _accepting[s] = data[offset] != 0;
            offset += 1;

            // 读取默认转移目标
            _defaultTransitions[s] = BitConverter.ToInt32(data.Slice(offset, 4));
            offset += 4;

            Int32 transitionCount = BitConverter.ToInt32(data.Slice(offset, 4));
            offset += 4;

            // 初始化转移数组，大小为 65536（覆盖所有 UInt16 字符）
            Int32[] transitionArray = new Int32[65536];

            // 如果有默认转移，先填充默认值；否则全部初始化为 -1
            if (_defaultTransitions[s] >= 0)
            {
                for (Int32 i = 0; i < 65536; i++)
                {
                    transitionArray[i] = _defaultTransitions[s];
                }
            }
            else
            {
                for (Int32 i = 0; i < 65536; i++)
                {
                    transitionArray[i] = -1;
                }
            }

            // 覆盖显式转移条目
            for (Int32 t = 0; t < transitionCount; t++)
            {
                UInt16 ch = BitConverter.ToUInt16(data.Slice(offset, 2));
                offset += 2;

                Int32 nextState = BitConverter.ToInt32(data.Slice(offset, 4));
                offset += 4;

                transitionArray[ch] = nextState;
            }

            _transitions[s] = transitionArray;
        }

        _encoding = encoding;
    }

    /// <summary>
    /// 获取指定状态在给定字符上的转移目标状态。
    /// </summary>
    private Boolean TryGetTransition(Int32 state, UInt16 ch, out Int32 nextState)
    {
        nextState = _transitions[state][ch];
        return nextState != -1;
    }

    /// <summary>
    /// 在输入切片的指定起始位置运行 DFA，返回最长匹配结果。
    /// </summary>
    private Match? TryMatchAt(ReadOnlySpan<Byte> input, Int32 startPos)
    {
        ReadOnlySpan<Byte> slice = input.Slice(startPos);
        CodePointIter iter = new CodePointIter(slice, _encoding);
        Int32 state = 0;
        Int32 consumedBytes = 0;
        Match? bestMatch = null;

        while (iter.TryNext(out Int32 byteLen, out Char ch))
        {
            if (!TryGetTransition(state, (UInt16)ch, out Int32 nextState))
            {
                break;
            }

            state = nextState;
            consumedBytes += byteLen;

            if (_accepting[state])
            {
                bestMatch = new Match(startPos, startPos + consumedBytes);
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// 判断输入中是否存在 DFA 匹配。
    /// </summary>
    public override Boolean IsMatch(ReadOnlySpan<Byte> input)
    {
        CodePointIter iter = new CodePointIter(input, _encoding);
        Int32 startPos = 0;

        while (startPos < input.Length)
        {
            Match? match = TryMatchAt(input, startPos);

            if (match is not null)
            {
                return true;
            }

            if (!iter.TryNext(out Int32 byteLen, out _))
            {
                break;
            }

            startPos += byteLen;
        }

        return false;
    }

    /// <summary>
    /// 查找输入中第一个 DFA 匹配区间。
    /// </summary>
    public override Match? FindFirst(ReadOnlySpan<Byte> input)
    {
        CodePointIter iter = new CodePointIter(input, _encoding);
        Int32 startPos = 0;

        while (startPos < input.Length)
        {
            Match? match = TryMatchAt(input, startPos);

            if (match is not null)
            {
                return match;
            }

            if (!iter.TryNext(out Int32 byteLen, out _))
            {
                break;
            }

            startPos += byteLen;
        }

        return null;
    }

    /// <summary>
    /// 查找输入中所有 DFA 匹配区间（非重叠，左到右最长匹配）。
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
        CodePointIter iter = new CodePointIter(input, _encoding);
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
                if (!iter.TryNext(out Int32 byteLen, out _))
                {
                    break;
                }

                startPos += byteLen;
            }
        }

        return matches;
    }
}
