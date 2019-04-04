using Nyar.Types;

namespace Nyar.VM.NyarVM.GC;

/// <summary>
///     基于 Executor 状态的 GC Root 提供者
///     从值栈、帧局部变量、堆全局变量中提取所有根引用
/// </summary>
public sealed class ExecutorGcRootProvider : IGcRootProvider
{
    private readonly Stack<Frame> _frame_stack;
    private readonly NyarHeap _heap;
    private readonly ValueStack _stack;

    /// <summary>
    ///     初始化 Executor GC Root 提供者
    /// </summary>
    /// <param name="stack">值栈。</param>
    /// <param name="frameStack">帧栈。</param>
    /// <param name="heap">堆。</param>
    public ExecutorGcRootProvider(ValueStack stack, Stack<Frame> frameStack, NyarHeap heap)
    {
        _stack = stack;
        _frame_stack = frameStack;
        _heap = heap;
    }

    /// <inheritdoc />
    public IEnumerable<Value> get_root_values()
    {
        foreach (var value in _stack.get_all_values()) yield return value;

        foreach (var frame in _frame_stack)
            for (var i = 0; i < frame.local_count; i++)
                yield return frame.get_local(i);

        foreach (var kvp in _heap.get_all_globals()) yield return kvp.Value;
    }
}