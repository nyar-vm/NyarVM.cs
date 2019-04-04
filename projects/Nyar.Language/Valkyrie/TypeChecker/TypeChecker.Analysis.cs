using Oak.Valkyrie.AST.Term;

namespace Nyar.Language.Valkyrie.TypeChecker;

/// <summary>
/// 可达性分析和确定性赋值分/// </summary>
public partial class TypeChecker
{
    private readonly HashSet<string> _assigned_locals = new(StringComparer.Ordinal);
    private bool _after_return;
    private bool _after_discard;
    private bool _after_break;
    private bool _after_continue;

    /// <summary>
    /// 重置块级分析状态（进入新语句块时调用）
    /// </summary>
    private void save_and_reset_block_state()
    {
        _saved_block_states.Push(new BlockState
        {
            assigned_locals = new HashSet<string>(_assigned_locals, StringComparer.Ordinal),
            after_return = _after_return,
            after_break = _after_break,
            after_continue = _after_continue
        });

        _after_return = false;
        _after_break = false;
        _after_continue = false;
    }

    /// <summary>
    /// 恢复块级分析状态（离开语句块时调用    /// </summary>
    private void restore_block_state()
    {
        if (_saved_block_states.TryPop(out var state))
        {
            _assigned_locals.IntersectWith(state.assigned_locals);
            _after_return = state.after_return;
            _after_break = state.after_break;
            _after_continue = state.after_continue;
        }
    }

    private readonly Stack<BlockState> _saved_block_states = new();

    /// <summary>
    /// 检查语句或声明是否可达
    /// </summary>
    private void check_reachable(AstNode node, string nodeLabel)
    {
        if (_after_return)
        {
            AddWarning("VALK2060", $"无法访问的代码：{nodeLabel} 位于 return 语句之后", node.Span);
            return;
        }

        if (_after_break)
        {
            AddWarning("VALK2061", $"无法访问的代码：{nodeLabel} 位于 break 语句之后", node.Span);
            return;
        }

        if (_after_continue)
        {
            AddWarning("VALK2062", $"无法访问的代码：{nodeLabel} 位于 continue 语句之后", node.Span);
            return;
        }

        if (_after_discard)
        {
            AddWarning("VALK2063", $"无法访问的代码：{nodeLabel} 位于 discard 语句之后", node.Span);
        }
    }

    /// <summary>
    /// 标记当前块中后续代码不可达（return 调用    /// </summary>
    private void mark_return()
    {
        _after_return = true;
    }

    /// <summary>
    /// 标记当前块中后续代码不可达（discard 调用    /// </summary>
    private void mark_discard()
    {
        _after_discard = true;
    }

    /// <summary>
    /// 标记当前块中后续代码不可达（break 调用    /// </summary>
    private void mark_break()
    {
        _after_break = true;
    }

    /// <summary>
    /// 标记当前块中后续代码不可达（continue 调用    /// </summary>
    private void mark_continue()
    {
        _after_continue = true;
    }

    /// <summary>
    /// 记录变量已赋    /// </summary>
    private void mark_assigned(string variableName)
    {
        _assigned_locals.Add(variableName);
    }

    /// <summary>
    /// 检查变量是否已赋值（如果未赋值且非参全局，报错）
    /// </summary>
    private void check_definitely_assigned(string variableName, TextSpan span)
    {
        if (!_assigned_locals.Contains(variableName))
        {
            var symbol = _currentScope.Resolve(variableName);
            if (symbol is not null && symbol.Kind == SymbolKind.Variable)
            {
                AddWarning("VALK2064", $"使用了可能未赋值的变量 '{variableName}'", span);
            }
        }
    }

    private struct BlockState
    {
        public HashSet<string> assigned_locals;
        public bool after_return;
        public bool after_break;
        public bool after_continue;
    }
}
