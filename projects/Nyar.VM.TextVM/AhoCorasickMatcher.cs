namespace Nyar.VM.TextVM;

/// <summary>
/// Aho-Corasick 多模式匹配自动机。在单次线性扫描中同时搜索多个模式串。
/// 用于字面量前缀集合的预过滤。
/// </summary>
public class AhoCorasickMatcher
{
    /// <summary>
    /// 失效链接表。_fail[state] 表示状态 state 的失效链接目标状态。
    /// </summary>
    private readonly Int32[] _fail;

    /// <summary>
    /// 转移函数表。_goto[state] 是一个字典，将输入字节映射到下一个状态。
    /// </summary>
    private readonly Dictionary<Byte, Int32>[] _goto;

    /// <summary>
    /// 输出表。_output[state] 表示到达该状态时匹配的模式索引列表。
    /// </summary>
    private readonly Int32[][] _output;

    /// <summary>
    /// 每个模式的长度，用于计算匹配位置。
    /// </summary>
    private readonly Int32[] _patternLengths;

    /// <summary>
    /// 使用给定的模式集合构建 AC 自动机。
    /// </summary>
    /// <param name="patterns">要搜索的模式串集合。</param>
    public AhoCorasickMatcher(IReadOnlyList<Byte[]> patterns)
    {
        if (patterns == null)
        {
            throw new ArgumentNullException(nameof(patterns));
        }

        // 存储模式长度以便后续计算匹配位置
        _patternLengths = new Int32[patterns.Count];

        for (Int32 idx = 0; idx < patterns.Count; idx++)
        {
            _patternLengths[idx] = patterns[idx]?.Length ?? 0;
        }

        // 步骤一：构建 Trie 树
        List<Dictionary<Byte, Int32>> gotoList =
        [
            new Dictionary<Byte, Int32>() // 状态 0 为根节点
        ];

        List<List<Int32>> outputList =
        [
            [] // 根节点的输出列表
        ];

        for (Int32 idx = 0; idx < patterns.Count; idx++)
        {
            Byte[] pattern = patterns[idx];

            if (pattern == null || pattern.Length == 0)
            {
                continue;
            }

            Int32 state = 0;

            for (Int32 j = 0; j < pattern.Length; j++)
            {
                Byte c = pattern[j];

                if (gotoList[state].TryGetValue(c, out Int32 nextState))
                {
                    state = nextState;
                }
                else
                {
                    Int32 newState = gotoList.Count;
                    gotoList.Add(new Dictionary<Byte, Int32>());
                    outputList.Add([]);
                    gotoList[state][c] = newState;
                    state = newState;
                }
            }

            outputList[state].Add(idx);
        }

        // 步骤二：构建失效链接（BFS）
        Int32 stateCount = gotoList.Count;
        Int32[] fail = new Int32[stateCount];
        Queue<Int32> queue = new Queue<Int32>();

        // 初始化深度为 1 的状态（根节点的直接子节点）
        foreach (KeyValuePair<Byte, Int32> entry in gotoList[0])
        {
            Int32 nextState = entry.Value;
            fail[nextState] = 0;
            queue.Enqueue(nextState);
        }

        // BFS 逐层构建失效链接
        while (queue.Count > 0)
        {
            Int32 state = queue.Dequeue();

            foreach (KeyValuePair<Byte, Int32> entry in gotoList[state])
            {
                Byte c = entry.Key;
                Int32 nextState = entry.Value;

                // 沿着失效链接回溯，寻找包含字符 c 的转移的状态
                Int32 f = fail[state];

                while (f != 0 && !gotoList[f].ContainsKey(c))
                {
                    f = fail[f];
                }

                if (gotoList[f].TryGetValue(c, out Int32 fNext))
                {
                    fail[nextState] = fNext;
                }
                else
                {
                    fail[nextState] = 0;
                }

                // 合并失效状态的输出
                outputList[nextState].AddRange(outputList[fail[nextState]]);

                queue.Enqueue(nextState);
            }
        }

        _goto = [.. gotoList];
        _fail = fail;

        _output = new Int32[stateCount][];

        for (Int32 i = 0; i < stateCount; i++)
        {
            _output[i] = [.. outputList[i]];
        }
    }

    /// <summary>
    /// 在文本中搜索所有模式，返回所有匹配的（模式索引，起始位置）对。
    /// </summary>
    /// <param name="text">待搜索的文本切片。</param>
    /// <returns>匹配结果序列，每个元素包含匹配的模式索引和起始位置。</returns>
    public IEnumerable<(Int32 PatternIndex, Int32 Position)> Search(ReadOnlySpan<Byte> text)
    {
        // 由于迭代器块无法捕获 ref struct（ReadOnlySpan<Byte>），此处复制到数组后委托给内部实现
        return SearchInternal([.. text]);
    }

    /// <summary>
    /// 搜索的内部实现，使用数组以避免 ref struct 捕获限制。
    /// </summary>
    private IEnumerable<(Int32 PatternIndex, Int32 Position)> SearchInternal(Byte[] text)
    {
        Int32 state = 0;

        for (Int32 i = 0; i < text.Length; i++)
        {
            Byte c = text[i];

            // 沿着失效链接回溯，直到找到包含字符 c 的转移的状态
            while (state != 0 && !_goto[state].ContainsKey(c))
            {
                state = _fail[state];
            }

            if (_goto[state].TryGetValue(c, out Int32 nextState))
            {
                state = nextState;
            }

            // 检查当前状态的输出
            Int32[] outputForState = _output[state];

            for (Int32 o = 0; o < outputForState.Length; o++)
            {
                Int32 patIdx = outputForState[o];
                Int32 position = i - _patternLengths[patIdx] + 1;

                yield return (patIdx, position);
            }
        }
    }

    /// <summary>
    /// 在文本中搜索，仅返回最早匹配的位置（或 -1 若无匹配）。
    /// 按搜索过程中首次遇到匹配的原则确定"最早"。
    /// </summary>
    /// <param name="text">待搜索的文本切片。</param>
    /// <returns>最早匹配的起始位置，未找到时返回 -1。</returns>
    public Int32 SearchFirst(ReadOnlySpan<Byte> text)
    {
        Int32 state = 0;

        for (Int32 i = 0; i < text.Length; i++)
        {
            Byte c = text[i];

            while (state != 0 && !_goto[state].ContainsKey(c))
            {
                state = _fail[state];
            }

            if (_goto[state].TryGetValue(c, out Int32 nextState))
            {
                state = nextState;
            }

            if (_output[state].Length > 0)
            {
                Int32 patIdx = _output[state][0];
                return i - _patternLengths[patIdx] + 1;
            }
        }

        return -1;
    }
}
