using System.Collections.Concurrent;
using Nyar.Types;
using Nyar.VM.NyarVM.Observability;

namespace Nyar.VM.NyarVM.Jit;

/// <summary>
///     栈上替换（OSR）管理器
///     负责检测循环回边热点、触发 OSR 编译、管理 OSR 入口点
/// </summary>
public sealed class OsrManager
{
    /// <summary>
    ///     循环回边计数器。
    ///     键：`(函数索引, 跳转目标代码字节偏移)`。
    /// </summary>
    private readonly ConcurrentDictionary<(int, int), int> _back_edge_counters = new();

    /// <summary>
    ///     已注册的 OSR 入口点。
    ///     键：`(函数索引, 入口代码字节偏移)`。
    /// </summary>
    private readonly ConcurrentDictionary<(int, int), OsrEntry> _osr_entries = new();

    /// <summary>
    ///     事件系统（可选）
    /// </summary>
    private VmEvents? _events;

    /// <summary>
    ///     指标收集器（可选）
    /// </summary>
    private VmMetrics? _metrics;

    /// <summary>
    ///     OSR 编译次数
    /// </summary>
    private int _osr_compile_count;

    /// <summary>
    ///     OSR 迁移次数
    /// </summary>
    private int _osr_transition_count;

    /// <summary>
    ///     OSR 触发阈值：循环回边执行次数达到此值后触发 OSR 编译
    /// </summary>
    public int osr_threshold { get; set; } = 500;

    /// <summary>
    ///     是否启用 OSR
    /// </summary>
    public bool enabled { get; set; } = true;

    /// <summary>
    ///     记录循环回边，返回是否应触发 OSR 编译
    /// </summary>
    /// <param name="functionIndex">函数索引。</param>
    /// <param name="jumpTargetPc">跳转目标代码字节偏移（循环入口）。</param>
    /// <param name="currentPc">当前跳转指令的代码字节偏移。</param>
    /// <returns>是否应触发 OSR。</returns>
    public bool record_back_edge(int functionIndex, int jumpTargetPc, int currentPc)
    {
        if (!enabled) return false;

        if (jumpTargetPc >= currentPc) return false;

        var key = (functionIndex, jumpTargetPc);
        var count = _back_edge_counters.AddOrUpdate(key, 1, (_, v) => v + 1);

        if (count == osr_threshold) return true;

        return false;
    }

    /// <summary>
    ///     注册 OSR 入口点
    /// </summary>
    /// <param name="functionIndex">函数索引。</param>
    /// <param name="entryPc">入口代码字节偏移。</param>
    /// <param name="stackDepth">栈深度。</param>
    /// <param name="localCount">局部变量数量。</param>
    /// <returns>注册的 OSR 入口点。</returns>
    public OsrEntry register_osr_entry(int functionIndex, int entryPc, int stackDepth, int localCount)
    {
        var key = (functionIndex, entryPc);
        return _osr_entries.GetOrAdd(key, _ => new OsrEntry(functionIndex, entryPc, stackDepth, localCount));
    }

    /// <summary>
    ///     查找已编译的 OSR 入口点
    /// </summary>
    /// <param name="functionIndex">函数索引。</param>
    /// <param name="entryPc">入口代码字节偏移。</param>
    /// <returns>OSR 入口点，如果不存在或未编译则返回 null。</returns>
    public OsrEntry? find_compiled_osr_entry(int functionIndex, int entryPc)
    {
        var key = (functionIndex, entryPc);
        if (_osr_entries.TryGetValue(key, out var entry) && entry.is_compiled) return entry;

        return null;
    }

    /// <summary>
    ///     通知 OSR 编译完成
    /// </summary>
    /// <param name="entry">OSR 入口点。</param>
    /// <param name="compiledDelegate">编译后的委托。</param>
    public void OnOsrCompiled(OsrEntry entry, Func<Value[], Value[], Value> compiledDelegate)
    {
        entry._compiled_delegate = compiledDelegate;
        Interlocked.Increment(ref _osr_compile_count);
        _metrics?.record_osr_compilation();
        _events?.OnOsrCompilationCompleted(entry.function_index, entry.bytecode_pc);
    }

    /// <summary>
    ///     通知 OSR 迁移完成
    /// </summary>
    public void OnOsrTransition()
    {
        Interlocked.Increment(ref _osr_transition_count);
        _metrics?.record_osr_transition();
        _events?.OnOsrTransition(0, 0);
    }

    /// <summary>
    ///     获取 OSR 统计信息
    /// </summary>
    /// <returns>统计信息字符串。</returns>
    public string get_statistics()
    {
        return $"OSR 编译: {_osr_compile_count}, OSR 迁移: {_osr_transition_count}, " +
               $"回边计数器: {_back_edge_counters.Count}, 入口点: {_osr_entries.Count}";
    }

    /// <summary>
    ///     清空所有计数器和入口点
    /// </summary>
    public void clear()
    {
        _back_edge_counters.Clear();
        _osr_entries.Clear();
        _osr_compile_count = 0;
        _osr_transition_count = 0;
    }

    /// <summary>
    ///     设置可观测性组件（指标收集器和事件系统）
    /// </summary>
    /// <param name="metrics">指标收集器（可选）。</param>
    /// <param name="events">事件系统（可选）。</param>
    public void set_observability(VmMetrics? metrics, VmEvents? events)
    {
        _metrics = metrics;
        _events = events;
    }
}
