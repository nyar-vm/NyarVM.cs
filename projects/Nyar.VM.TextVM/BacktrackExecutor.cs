namespace Nyar.VM.TextVM;

/// <summary>
/// 基于回溯的虚拟机执行器。支持捕获组和反向引用。
/// 通过 CancellationToken 提供超时保护，用于 ContextFree 复杂度的模式。
/// </summary>
public class BacktrackExecutor : CompiledUnit
{
    private readonly Inst[] _program;
    private readonly Int32 _captureCount;
    private readonly Int32 _backtrackTimeoutMs;

    /// <summary>
    /// 创建回溯执行器。
    /// </summary>
    /// <param name="program">已编译的指令列表。</param>
    /// <param name="captureCount">捕获组数量。</param>
    /// <param name="backtrackTimeoutMs">超时毫秒数。</param>
    public BacktrackExecutor(Inst[] program, Int32 captureCount, Int32 backtrackTimeoutMs = 100)
    {
        _program = program;
        _captureCount = captureCount;
        _backtrackTimeoutMs = backtrackTimeoutMs;
    }

    /// <inheritdoc />
    public override Boolean IsMatch(ReadOnlySpan<Byte> input)
    {
        return FindFirst(input) is not null;
    }

    /// <inheritdoc />
    public override Match? FindFirst(ReadOnlySpan<Byte> input)
    {
        using CancellationTokenSource cts = new CancellationTokenSource(_backtrackTimeoutMs);
        Int32[] captures = new Int32[_captureCount * 2];
        Int32 startPos = 0;
        CodePointIter iter = new CodePointIter(input, TextEncoding.Utf8);

        while (startPos <= input.Length)
        {
            // 定位到起始位置
            iter.Seek(startPos);

            if (MatchAt(input, 0, startPos, captures, iter, cts.Token))
            {
                Int32 endPos = captures.Length > 1 ? captures[1] : startPos;
                if (endPos > startPos)
                {
                    return new Match(startPos, endPos);
                }

                // 零长度匹配：向前推进一个码点
                if (iter.TryNext(out Int32 len, out _))
                {
                    startPos += len;
                    continue;
                }

                break;
            }

            // 匹配失败：推进一个码点
            iter.Seek(startPos);
            if (iter.TryNext(out Int32 advanceLen, out _))
            {
                startPos += advanceLen;
            }
            else
            {
                break;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public override IEnumerable<Match> FindAll(ReadOnlySpan<Byte> input)
    {
        List<Match> results = [];
        Int32 pos = 0;

        while (pos <= input.Length)
        {
            Match? match = FindFirst(input.Slice(pos));
            if (match is null)
            {
                break;
            }

            Match adjusted = new Match(pos + match.Value.Start, pos + match.Value.End);
            results.Add(adjusted);

            if (match.Value.Length == 0)
            {
                pos += 1;
            }
            else
            {
                pos += match.Value.End;
            }
        }

        return results;
    }

    /// <inheritdoc />
    public override Byte[] Replace(ReadOnlySpan<Byte> input, ReadOnlySpan<Byte> replacement)
    {
        List<Match> matches = [.. FindAll(input)];
        if (matches.Count == 0)
        {
            return [.. input];
        }

        Int32 totalLen = input.Length;
        foreach (Match m in matches)
        {
            totalLen += replacement.Length - m.Length;
        }

        Byte[] result = new Byte[totalLen];
        Int32 resultPos = 0;
        Int32 inputPos = 0;

        foreach (Match m in matches)
        {
            // Copy content before match
            Int32 beforeLen = m.Start - inputPos;
            input.Slice(inputPos, beforeLen).CopyTo(result.AsSpan(resultPos));
            resultPos += beforeLen;

            // Copy replacement
            replacement.CopyTo(result.AsSpan(resultPos));
            resultPos += replacement.Length;

            inputPos = m.End;
        }

        // Copy remaining content after last match
        input.Slice(inputPos).CopyTo(result.AsSpan(resultPos));

        return result;
    }

    /// <summary>
    /// 递归回溯匹配引擎。
    /// </summary>
    internal Boolean MatchAt(
        ReadOnlySpan<Byte> input, Int32 pc, Int32 pos,
        Int32[] captures, CodePointIter iter, CancellationToken ct, Int32 depth = 0)
    {
        if (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(ct);
        }

        // 防止栈溢出的硬限制，主要保护仍由 CancellationToken 超时提供
        if (depth > 500)
        {
            return false;
        }

        while (pc < _program.Length)
        {
            Inst inst = _program[pc];

            switch (inst.Kind)
            {
                case InstKind.Char:
                {
                    if (!iter.TryNext(out Int32 byteLen, out Char ch) || ch != (Char)inst.IntArg1)
                    {
                        return false;
                    }

                    pc++;
                    pos += byteLen;
                    continue;
                }

                case InstKind.Any:
                {
                    if (!iter.TryNext(out Int32 byteLen, out _))
                    {
                        return false;
                    }

                    pc++;
                    pos += byteLen;
                    continue;
                }

                case InstKind.CharClass:
                {
                    if (!iter.TryNext(out Int32 byteLen, out Char ch))
                    {
                        return false;
                    }

                    Boolean matched = false;
                    foreach (CharRange range in inst.Ranges)
                    {
                        if (ch >= range.Lo && ch <= range.Hi)
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (inst.Negated)
                    {
                        matched = !matched;
                    }

                    if (!matched)
                    {
                        return false;
                    }

                    pc++;
                    pos += byteLen;
                    continue;
                }

                case InstKind.Split:
                {
                    // Save state for backtracking
                    Int32 savedPos = pos;
                    CodePointIter savedIter = iter; // struct copy

                    if (MatchAt(input, inst.IntArg1, pos, captures, iter, ct, depth + 1))
                    {
                        return true;
                    }

                    // Restore state and try right branch
                    pos = savedPos;
                    iter = savedIter;

                    // Restore captures (copy from savedCaptures)
                    if (MatchAt(input, inst.IntArg2, pos, captures, iter, ct, depth + 1))
                    {
                        return true;
                    }

                    return false;
                }

                case InstKind.Save:
                {
                    captures[inst.IntArg1] = pos;
                    pc++;
                    continue;
                }

                case InstKind.Jump:
                {
                    pc = inst.IntArg1;
                    continue;
                }

                case InstKind.Match:
                {
                    return true;
                }

                case InstKind.Fail:
                {
                    return false;
                }

                case InstKind.Backref:
                {
                    Int32 groupStart = captures[inst.IntArg1 * 2];
                    Int32 groupEnd = captures[inst.IntArg1 * 2 + 1];
                    Int32 groupLen = groupEnd - groupStart;

                    if (groupLen <= 0)
                    {
                        pc++;
                        continue;
                    }

                    if (pos + groupLen > input.Length)
                    {
                        return false;
                    }

                    if (!input.Slice(pos, groupLen).SequenceEqual(input.Slice(groupStart, groupLen)))
                    {
                        return false;
                    }

                    // Advance iter by the group's byte length
                    Int32 advanced = 0;
                    while (advanced < groupLen)
                    {
                        if (!iter.TryNext(out Int32 byteLen, out _))
                        {
                            return false;
                        }

                        advanced += byteLen;
                    }

                    pc++;
                    pos += groupLen;
                    continue;
                }

                case InstKind.Anchor:
                {
                    Boolean anchorMatch = inst.IntArg1 switch
                    {
                        0 => pos == 0,                                    // ^
                        1 => pos >= input.Length,                          // $
                        2 => pos > 0 && pos < input.Length &&              // \b
                             (IsWordChar(input[pos - 1]) != IsWordChar(input[pos])),
                        _ => false,
                    };

                    if (!anchorMatch)
                    {
                        return false;
                    }

                    pc++;
                    continue;
                }

                default:
                    return false;
            }
        }

        return false;
    }

    private static Boolean IsWordChar(Byte b)
    {
        return (b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') ||
               (b >= '0' && b <= '9') || b == '_';
    }
}
