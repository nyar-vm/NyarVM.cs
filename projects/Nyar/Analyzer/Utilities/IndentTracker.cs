namespace Nyar.Analyzer.Utilities;

/// <summary>
///     缩进追踪器，为缩进敏感语言提供 INDENT/DEDENT 逻辑
/// </summary>
public sealed class IndentTracker
{
    private readonly Stack<int> _indent_stack = new();

    /// <summary>
    ///     初始化缩进追踪器，初始缩进级别为 0
    /// </summary>
    public IndentTracker()
    {
        _indent_stack.Push(0);
    }

    /// <summary>
    ///     当前缩进级别栈
    /// </summary>
    public IReadOnlyList<int> indent_levels => [.. _indent_stack];

    /// <summary>
    ///     当前缩进级别
    /// </summary>
    public int current_indent => _indent_stack.Count > 0 ? _indent_stack.Peek() : 0;

    /// <summary>
    ///     处理一行的缩进，返回产生的缩进事件列表
    /// </summary>
    /// <param name="indentLevel">当前行的缩进级别（空格数或制表符等效数）。</param>
    /// <returns>缩进事件列表。</returns>
    public IReadOnlyList<IndentEvent> process_line(int indentLevel)
    {
        var events = new List<IndentEvent>();
        var current = _indent_stack.Peek();

        if (indentLevel > current)
        {
            _indent_stack.Push(indentLevel);
            events.Add(IndentEvent.indent);
        }
        else if (indentLevel < current)
        {
            while (_indent_stack.Count > 1 && _indent_stack.Peek() > indentLevel)
            {
                _indent_stack.Pop();
                events.Add(IndentEvent.dedent);
            }

            if (_indent_stack.Peek() != indentLevel)
            {
                _indent_stack.Push(indentLevel);
                events.Add(IndentEvent.indent);
            }
        }

        return events;
    }

    /// <summary>
    ///     重置追踪器到初始状态
    /// </summary>
    public void reset()
    {
        _indent_stack.Clear();
        _indent_stack.Push(0);
    }

    /// <summary>
    ///     产生文件末尾所需的剩余 DEDENT 事件
    /// </summary>
    /// <returns>DEDENT 事件列表。</returns>
    public IReadOnlyList<IndentEvent> close()
    {
        var events = new List<IndentEvent>();
        while (_indent_stack.Count > 1)
        {
            _indent_stack.Pop();
            events.Add(IndentEvent.dedent);
        }

        return events;
    }
}