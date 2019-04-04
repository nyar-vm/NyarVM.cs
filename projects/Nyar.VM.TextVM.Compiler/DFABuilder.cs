using System.Buffers.Binary;
using System.Collections.Immutable;
using Nyar.VM.TextVM;

namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// DFA 状态，包含 AST 节点（语言表示）和接受状态标记。
/// </summary>
public readonly struct DFAState
{
    /// <summary>
    /// 状态编号。
    /// </summary>
    public Int32 Id { get; init; }

    /// <summary>
    /// 状态对应的 AST 节点（规范化后的语言表示）。
    /// </summary>
    public AstNode? Node { get; init; }

    /// <summary>
    /// 是否为接受状态。
    /// </summary>
    public Boolean IsAccepting { get; init; }
}

/// <summary>
/// 基于 Brzozowski 导数的惰性 DFA 构造器。
/// 状态 = 规范化后的 AST 节点；转移 = 对每个需考虑的字符取导数。
/// </summary>
public class DFABuilder
{
    private readonly AstNode _root;
    private readonly List<AstNode?> _states;
    private readonly Dictionary<AstNode, Int32> _stateIndex;
    private readonly List<Dictionary<Char, Int32>> _transitions;
    private readonly List<Int32> _defaultTransitions;
    private readonly List<Boolean> _accepting;
    private readonly Int32 _maxStates;

    /// <summary>
    /// null 节点（表示空语言/拒绝状态）的占位哨兵。
    /// 因为 Dictionary 不允许 null 键，使用此哨兵代替。
    /// </summary>
    private static readonly AstNode NullSentinel = new LiteralNode("__null_sentinel__");
    private List<Char>? _probeChars;
    private List<CharRange>? _classRanges;

    /// <summary>
    /// 创建 DFA 构造器。
    /// </summary>
    /// <param name="root">已规范化的 AST 根节点。</param>
    /// <param name="maxStates">最大状态数量，超出此限制时将停止构建。</param>
    public DFABuilder(AstNode root, Int32 maxStates = 1024)
    {
        _root = root;
        _maxStates = maxStates;
        _states = [];
        _stateIndex = new Dictionary<AstNode, Int32>(Derivative.EqualityComparer);
        _transitions = [];
        _defaultTransitions = [];
        _accepting = [];
    }

    /// <summary>
    /// 构建 DFA，返回是否在限制内完成。
    /// </summary>
    public Boolean Build()
    {
        _probeChars = [];
        _classRanges = [];
        CollectAlphabet(_root);

        Int32 startId = GetOrCreateState(_root);
        Queue<Int32> queue = new Queue<Int32>();
        queue.Enqueue(startId);

        Char defaultChar = FindDefaultChar();

        while (queue.Count > 0 && _states.Count < _maxStates)
        {
            Int32 stateId = queue.Dequeue();
            AstNode? node = _states[stateId];

            foreach (Char probe in _probeChars)
            {
                AstNode? derived = node is null ? null : Derivative.Derive(node, probe);
                derived = Derivative.Normalize(derived);

                Int32 before = _states.Count;
                Int32 targetId = GetOrCreateState(derived);

                _transitions[stateId][probe] = targetId;

                if (_states.Count > before)
                {
                    // 新创建的状态需要加入队列继续构建其转移
                    queue.Enqueue(targetId);
                }
            }

            // 处理默认字符（不在探针集合中的字符）
            // 默认转移用于所有非探针字符的转移，计算结果为 ∅ 时设置为 -1（无转移）。
            if (!_probeChars.Contains(defaultChar))
            {
                AstNode? defaultDerived = node is null ? null : Derivative.Derive(node, defaultChar);
                defaultDerived = Derivative.Normalize(defaultDerived);

                if (defaultDerived is not null)
                {
                    Int32 before = _states.Count;
                    Int32 defaultTargetId = GetOrCreateState(defaultDerived);

                    if (_states.Count > before)
                    {
                        queue.Enqueue(defaultTargetId);
                    }

                    _defaultTransitions[stateId] = defaultTargetId;
                }
                else
                {
                    // 默认字符导数为 ∅，设置 -1 表示无默认转移
                    _defaultTransitions[stateId] = -1;
                }
            }
        }

        return _states.Count <= _maxStates;
    }

    /// <summary>
    /// 获取 DFA 状态数量。
    /// </summary>
    public Int32 StateCount => _states.Count;

    /// <summary>
    /// 获取 DFA 转移表。返回 state -> (char -> nextState) 的映射。
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<Char, Int32>> GetTransitionTable()
    {
        return [.. _transitions.Select(t => (IReadOnlyDictionary<Char, Int32>)t)];
    }

    /// <summary>
    /// 获取接受状态集合。
    /// </summary>
    public IReadOnlySet<Int32> GetAcceptingStates()
    {
        HashSet<Int32> accepting = [];

        for (Int32 i = 0; i < _accepting.Count; i++)
        {
            if (_accepting[i])
            {
                accepting.Add(i);
            }
        }

        return accepting;
    }

    /// <summary>
    /// 获取所有 DFA 状态的只读列表。
    /// </summary>
    public IReadOnlyList<DFAState> GetStates()
    {
        List<DFAState> states = new List<DFAState>(_states.Count);

        for (Int32 i = 0; i < _states.Count; i++)
        {
            states.Add(new DFAState
            {
                Id = i,
                Node = _states[i],
                IsAccepting = _accepting[i],
            });
        }

        return states;
    }

    /// <summary>
    /// 将 DFA 序列化为裸表数据（不含 TvmHeader）。
    /// 格式：stateCount(4B) + 每个状态(accepting(1B) + defaultTransition(4B) + transitionCount(4B) + entries)。
    /// </summary>
    public Byte[] Serialize()
    {
        Int32 dfaTableSize = CalculateDfaTableSize();
        Byte[] buffer = new Byte[dfaTableSize];
        Span<Byte> span = buffer.AsSpan();
        WriteDfaTable(span);
        return buffer;
    }

    /// <summary>
    /// 计算 DFA 表序列化后的字节数。
    /// </summary>
    private Int32 CalculateDfaTableSize()
    {
        Int32 size = 4; // stateCount

        for (Int32 i = 0; i < _states.Count; i++)
        {
            size += 1;     // accepting
            size += 4;     // defaultTransition
            size += 4;     // transitionCount

            List<(UInt16 Ch, Int32 Next)> entries = GetTransitionEntries(i);

            foreach ((UInt16 _, Int32 _) in entries)
            {
                size += 2 + 4; // ch(2) + nextState(4)
            }
        }

        return size;
    }

    /// <summary>
    /// 将 DFA 表数据写入目标 Span。
    /// </summary>
    private void WriteDfaTable(Span<Byte> destination)
    {
        Int32 stateCount = _states.Count;
        BinaryPrimitives.WriteInt32LittleEndian(destination, stateCount);
        Int32 offset = 4;

        for (Int32 i = 0; i < stateCount; i++)
        {
            // 写入 accepting 标志
            destination[offset] = _accepting[i] ? (Byte)1 : (Byte)0;
            offset += 1;

            // 写入默认转移目标（-1 表示无默认转移）
            BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(offset), _defaultTransitions[i]);
            offset += 4;

            // 获取转移条目（仅包含显式探针字符的转移）
            List<(UInt16 Ch, Int32 Next)> entries = GetTransitionEntries(i);
            Int32 transitionCount = entries.Count;

            // 写入转移数量
            BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(offset), transitionCount);
            offset += 4;

            // 写入每条转移
            for (Int32 t = 0; t < transitionCount; t++)
            {
                (UInt16 ch, Int32 next) = entries[t];
                BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset), ch);
                offset += 2;
                BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(offset), next);
                offset += 4;
            }
        }
    }

    /// <summary>
    /// 获取指定状态的显式转移条目列表（不包含默认转移）。
    /// </summary>
    private List<(UInt16 Ch, Int32 Next)> GetTransitionEntries(Int32 stateId)
    {
        Dictionary<Char, Int32> stateTransitions = _transitions[stateId];

        List<(UInt16 Ch, Int32 Next)> entries = new List<(UInt16, Int32)>(stateTransitions.Count);

        foreach (KeyValuePair<Char, Int32> kv in stateTransitions)
        {
            entries.Add(((UInt16)kv.Key, kv.Value));
        }

        return entries;
    }

    /// <summary>
    /// 从序列化数据反序列化 DFA 表（含 defaultTransition 格式）。
    /// </summary>
    public static (Int32 stateCount, Boolean[] accepting, Int32[] defaultTransitions, Dictionary<UInt16, Int32>[] transitions) Deserialize(
        ReadOnlySpan<Byte> data)
    {
        Int32 offset = 0;
        Int32 stateCount = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;

        Boolean[] accepting = new Boolean[stateCount];
        Int32[] defaultTransitions = new Int32[stateCount];
        Dictionary<UInt16, Int32>[] transitions = new Dictionary<UInt16, Int32>[stateCount];

        for (Int32 i = 0; i < stateCount; i++)
        {
            accepting[i] = data[offset] != 0;
            offset += 1;

            defaultTransitions[i] = BitConverter.ToInt32(data.Slice(offset, 4));
            offset += 4;

            Int32 transitionCount = BitConverter.ToInt32(data.Slice(offset, 4));
            offset += 4;

            Dictionary<UInt16, Int32> trans = new Dictionary<UInt16, Int32>(transitionCount);
            transitions[i] = trans;

            for (Int32 t = 0; t < transitionCount; t++)
            {
                UInt16 ch = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
                offset += 2;

                Int32 nextState = BitConverter.ToInt32(data.Slice(offset, 4));
                offset += 4;

                trans[ch] = nextState;
            }
        }

        return (stateCount, accepting, defaultTransitions, transitions);
    }

    /// <summary>
    /// 添加新状态，返回其编号。
    /// </summary>
    private Int32 AddState(AstNode? node)
    {
        Int32 id = _states.Count;
        _states.Add(node);
        _accepting.Add(node is not null && Derivative.IsNullable(node));
        _transitions.Add(new Dictionary<Char, Int32>());
        _defaultTransitions.Add(-1);

        return id;
    }

    /// <summary>
    /// 获取或创建状态。若节点已存在则返回现有编号，否则创建新状态。
    /// null 节点通过哨兵对象处理。
    /// </summary>
    private Int32 GetOrCreateState(AstNode? node)
    {
        AstNode key = node ?? NullSentinel;

        if (_stateIndex.TryGetValue(key, out Int32 existingId))
        {
            return existingId;
        }

        Int32 newId = AddState(node);
        _stateIndex[key] = newId;

        return newId;
    }

    /// <summary>
    /// 收集 AST 中的字符信息，构建探针字符集和字符类区间。
    /// </summary>
    private void CollectAlphabet(AstNode node)
    {
        if (_probeChars is null || _classRanges is null)
        {
            return;
        }

        HashSet<Char> probeSet = [];
        WalkAlphabet(node, probeSet, _classRanges);

        _probeChars.Clear();
        _probeChars.AddRange(probeSet);
        _probeChars.Sort();
    }

    /// <summary>
    /// 递归遍历 AST 收集字符信息。
    /// </summary>
    private static void WalkAlphabet(
        AstNode node,
        HashSet<Char> probeChars,
        List<CharRange> classRanges)
    {
        switch (node)
        {
            case LiteralNode literal:
            {
                foreach (Char ch in literal.Value)
                {
                    probeChars.Add(ch);
                }

                break;
            }

            case CharClassNode charClass:
            {
                classRanges.AddRange(charClass.Ranges);

                foreach (CharRange range in charClass.Ranges)
                {
                    Int32 rangeSize = range.Hi - range.Lo;

                    if (rangeSize >= 0 && rangeSize <= 256)
                    {
                        for (Char ch = range.Lo; ch <= range.Hi; ch++)
                        {
                            probeChars.Add(ch);
                        }
                    }
                    else
                    {
                        probeChars.Add(range.Lo);
                        probeChars.Add(range.Hi);

                        if (rangeSize > 1)
                        {
                            Char mid = (Char)(range.Lo + rangeSize / 2);
                            probeChars.Add(mid);
                        }
                    }
                }

                break;
            }

            case ConcatNode concat:
            {
                foreach (AstNode child in concat.Children)
                {
                    WalkAlphabet(child, probeChars, classRanges);
                }

                break;
            }

            case AltNode alt:
            {
                WalkAlphabet(alt.Left, probeChars, classRanges);
                WalkAlphabet(alt.Right, probeChars, classRanges);
                break;
            }

            case StarNode star:
            {
                WalkAlphabet(star.Inner, probeChars, classRanges);
                break;
            }

            case PlusNode plus:
            {
                WalkAlphabet(plus.Inner, probeChars, classRanges);
                break;
            }

            case OptionalNode opt:
            {
                WalkAlphabet(opt.Inner, probeChars, classRanges);
                break;
            }

            case IntersectNode inter:
            {
                WalkAlphabet(inter.Left, probeChars, classRanges);
                WalkAlphabet(inter.Right, probeChars, classRanges);
                break;
            }

            case ComplementNode comp:
            {
                WalkAlphabet(comp.Inner, probeChars, classRanges);
                break;
            }

            case DifferenceNode diff:
            {
                WalkAlphabet(diff.Left, probeChars, classRanges);
                WalkAlphabet(diff.Right, probeChars, classRanges);
                break;
            }

            case CaptureNode cap:
            {
                WalkAlphabet(cap.Inner, probeChars, classRanges);
                break;
            }

            case AnyNode:
            case BackrefNode:
            case AnchorNode:
                break;
        }
    }

    /// <summary>
    /// 查找不在探针集合中的字符作为默认字符。
    /// </summary>
    private Char FindDefaultChar()
    {
        if (_probeChars is null || _probeChars.Count == 0)
        {
            return '\0';
        }

        // 使用 HashSet 加速查找
        HashSet<Char> probeSet = [.. _probeChars];

        for (Int32 i = 0; i <= Char.MaxValue; i++)
        {
            Char ch = (Char)i;

            if (!probeSet.Contains(ch))
            {
                return ch;
            }
        }

        return '\0';
    }

    /// <summary>
    /// 构建 DOT 格式的 DFA 可视化描述（调试用）。
    /// </summary>
    public String ToDotFormat()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("digraph DFA {");
        sb.AppendLine("    rankdir=LR;");

        for (Int32 i = 0; i < _states.Count; i++)
        {
            String shape = _accepting[i] ? "doublecircle" : "circle";
            sb.AppendLine($"    {i} [shape={shape}];");
        }

        for (Int32 i = 0; i < _states.Count; i++)
        {
            foreach (KeyValuePair<Char, Int32> kv in _transitions[i])
            {
                String label = kv.Key == ' ' ? "' '" : kv.Key.ToString();
                sb.AppendLine($"    {i} -> {kv.Value} [label=\"{label}\"];");
            }

            if (_defaultTransitions[i] >= 0)
            {
                sb.AppendLine($"    {i} -> {_defaultTransitions[i]} [label=\"*\"];");
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
}
