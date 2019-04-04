using System.Diagnostics;
using System.Text;

namespace Nyar.VM.NyarVM.Observability;

/// <summary>
///     VM 运行时统一指标收集器，聚合 GC/JIT/OSR/执行器各组件的统计数据
/// </summary>
public sealed class VmMetrics
{
    #region 执行器指标

    /// <summary>
    ///     累计执行指令数
    /// </summary>
    public long total_instructions => _total_instructions;

    /// <summary>
    ///     累计函数调用次数
    /// </summary>
    public long total_function_calls => _total_function_calls;

    /// <summary>
    ///     累计解释执行时间（毫秒）
    /// </summary>
    public double total_execution_time_ms => _execution_stopwatch.Elapsed.TotalMilliseconds;

    private long _total_instructions;
    private long _total_function_calls;
    private readonly Stopwatch _execution_stopwatch = new();

    #endregion

    #region JIT 指标

    /// <summary>
    ///     JIT 编译次数
    /// </summary>
    public int jit_compilation_count => _jit_compilation_count;

    /// <summary>
    ///     JIT 编译总耗时（毫秒）
    /// </summary>
    public double jit_total_compilation_time_ms { get; private set; }

    /// <summary>
    ///     JIT 编译后函数执行次数
    /// </summary>
    public long jit_execution_count => _jit_execution_count;

    /// <summary>
    ///     内联缓存命中次数
    /// </summary>
    public long ic_hit_count => _ic_hit_count;

    /// <summary>
    ///     内联缓存未命中次数
    /// </summary>
    public long ic_miss_count => _ic_miss_count;

    /// <summary>
    ///     内联缓存命中率
    /// </summary>
    public double ic_hit_rate => _ic_hit_count + _ic_miss_count > 0
        ? (double)_ic_hit_count / (_ic_hit_count + _ic_miss_count)
        : 0.0;

    private int _jit_compilation_count;
    private long _jit_execution_count;
    private long _ic_hit_count;
    private long _ic_miss_count;

    #endregion

    #region GC 指标

    /// <summary>
    ///     Major GC 执行次数
    /// </summary>
    public long gc_major_collections => _gc_major_collections;

    /// <summary>
    ///     Minor GC 执行次数
    /// </summary>
    public long gc_minor_collections => _gc_minor_collections;

    /// <summary>
    ///     GC 总暂停时间（毫秒）
    /// </summary>
    public double gc_total_pause_time_ms { get; private set; }

    /// <summary>
    ///     GC 最后一次暂停时间（毫秒）
    /// </summary>
    public double gc_last_pause_time_ms { get; private set; }

    /// <summary>
    ///     当前存活对象数
    /// </summary>
    public int gc_live_object_count { get; private set; }

    /// <summary>
    ///     GC 总回收对象数
    /// </summary>
    public long gc_total_reclaimed => _gc_total_reclaimed;

    /// <summary>
    ///     GC 晋升到老生代的对象总数
    /// </summary>
    public long gc_promoted_objects => _gc_promoted_objects;

    private long _gc_major_collections;
    private long _gc_minor_collections;
    private long _gc_total_reclaimed;
    private long _gc_promoted_objects;

    #endregion

    #region OSR 指标

    /// <summary>
    ///     OSR 编译次数
    /// </summary>
    public int osr_compilation_count => _osr_compilation_count;

    /// <summary>
    ///     OSR 迁移次数
    /// </summary>
    public int osr_transition_count => _osr_transition_count;

    private int _osr_compilation_count;
    private int _osr_transition_count;

    #endregion

    #region FFI 指标

    /// <summary>
    ///     Intrinsic 调用次数
    /// </summary>
    public long intrinsic_call_count => _intrinsic_call_count;

    /// <summary>
    ///     Native 调用次数
    /// </summary>
    public long native_call_count => _native_call_count;

    private long _intrinsic_call_count;
    private long _native_call_count;

    #endregion

    #region 记录方法

    /// <summary>
    ///     记录指令执行
    /// </summary>
    public void record_instruction()
    {
        Interlocked.Increment(ref _total_instructions);
    }

    /// <summary>
    ///     记录函数调用
    /// </summary>
    public void record_function_call()
    {
        Interlocked.Increment(ref _total_function_calls);
    }

    /// <summary>
    ///     记录 JIT 编译完成
    /// </summary>
    /// <param name="durationMs">编译耗时（毫秒）。</param>
    public void record_jit_compilation(double durationMs)
    {
        Interlocked.Increment(ref _jit_compilation_count);
        jit_total_compilation_time_ms += durationMs;
    }

    /// <summary>
    ///     记录 JIT 编译后函数执行
    /// </summary>
    public void record_jit_execution()
    {
        Interlocked.Increment(ref _jit_execution_count);
    }

    /// <summary>
    ///     记录内联缓存命中
    /// </summary>
    public void record_ic_hit()
    {
        Interlocked.Increment(ref _ic_hit_count);
    }

    /// <summary>
    ///     记录内联缓存未命中
    /// </summary>
    public void record_ic_miss()
    {
        Interlocked.Increment(ref _ic_miss_count);
    }

    /// <summary>
    ///     记录 Major GC 完成
    /// </summary>
    /// <param name="pauseMs">暂停时间（毫秒）。</param>
    /// <param name="liveCount">存活对象数。</param>
    /// <param name="reclaimed">回收对象数。</param>
    public void record_gc_major_collection(double pauseMs, int liveCount, long reclaimed)
    {
        Interlocked.Increment(ref _gc_major_collections);
        gc_total_pause_time_ms += pauseMs;
        gc_last_pause_time_ms = pauseMs;
        gc_live_object_count = liveCount;
        Interlocked.Add(ref _gc_total_reclaimed, reclaimed);
    }

    /// <summary>
    ///     记录 Minor GC 完成
    /// </summary>
    /// <param name="pauseMs">暂停时间（毫秒）。</param>
    /// <param name="liveCount">存活对象数。</param>
    /// <param name="reclaimed">回收对象数。</param>
    /// <param name="promoted">晋升对象数。</param>
    public void record_gc_minor_collection(double pauseMs, int liveCount, long reclaimed, long promoted)
    {
        Interlocked.Increment(ref _gc_minor_collections);
        gc_total_pause_time_ms += pauseMs;
        gc_last_pause_time_ms = pauseMs;
        gc_live_object_count = liveCount;
        Interlocked.Add(ref _gc_total_reclaimed, reclaimed);
        Interlocked.Add(ref _gc_promoted_objects, promoted);
    }

    /// <summary>
    ///     更新 GC 存活对象数（从 GC 同步）
    /// </summary>
    /// <param name="liveCount">存活对象数。</param>
    public void update_gc_live_count(int liveCount)
    {
        gc_live_object_count = liveCount;
    }

    /// <summary>
    ///     记录 OSR 编译完成
    /// </summary>
    public void record_osr_compilation()
    {
        Interlocked.Increment(ref _osr_compilation_count);
    }

    /// <summary>
    ///     记录 OSR 迁移
    /// </summary>
    public void record_osr_transition()
    {
        Interlocked.Increment(ref _osr_transition_count);
    }

    /// <summary>
    ///     记录 Intrinsic 调用
    /// </summary>
    public void record_intrinsic_call()
    {
        Interlocked.Increment(ref _intrinsic_call_count);
    }

    /// <summary>
    ///     记录 Native 调用
    /// </summary>
    public void record_native_call()
    {
        Interlocked.Increment(ref _native_call_count);
    }

    /// <summary>
    ///     开始执行计时
    /// </summary>
    public void start_execution_timer()
    {
        _execution_stopwatch.Restart();
    }

    /// <summary>
    ///     停止执行计时
    /// </summary>
    public void stop_execution_timer()
    {
        _execution_stopwatch.Stop();
    }

    #endregion

    #region 快照与报告

    /// <summary>
    ///     获取当前指标快照
    /// </summary>
    /// <returns>指标快照。</returns>
    public VmMetricsSnapshot get_snapshot()
    {
        return new VmMetricsSnapshot
        {
            total_instructions = _total_instructions,
            total_function_calls = _total_function_calls,
            total_execution_time_ms = _execution_stopwatch.Elapsed.TotalMilliseconds,
            jit_compilation_count = _jit_compilation_count,
            jit_total_compilation_time_ms = jit_total_compilation_time_ms,
            jit_execution_count = _jit_execution_count,
            ic_hit_count = _ic_hit_count,
            ic_miss_count = _ic_miss_count,
            ic_hit_rate = ic_hit_rate,
            gc_major_collections = _gc_major_collections,
            gc_minor_collections = _gc_minor_collections,
            gc_total_pause_time_ms = gc_total_pause_time_ms,
            gc_last_pause_time_ms = gc_last_pause_time_ms,
            gc_live_object_count = gc_live_object_count,
            gc_total_reclaimed = _gc_total_reclaimed,
            gc_promoted_objects = _gc_promoted_objects,
            osr_compilation_count = _osr_compilation_count,
            osr_transition_count = _osr_transition_count,
            intrinsic_call_count = _intrinsic_call_count,
            native_call_count = _native_call_count
        };
    }

    /// <summary>
    ///     生成格式化的指标报告
    /// </summary>
    /// <returns>格式化报告字符串。</returns>
    public string get_report()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== NyarVM 运行时指标 ===");

        sb.AppendLine();
        sb.AppendLine("── 执行器 ──");
        sb.AppendLine($"  指令数: {_total_instructions:N0}");
        sb.AppendLine($"  函数调用: {_total_function_calls:N0}");
        sb.AppendLine($"  执行时间: {_execution_stopwatch.Elapsed.TotalMilliseconds:F2}ms");

        var ips = _execution_stopwatch.Elapsed.TotalMilliseconds > 0
            ? _total_instructions / (_execution_stopwatch.Elapsed.TotalMilliseconds / 1000.0)
            : 0;
        sb.AppendLine($"  IPS: {ips:N0}");

        sb.AppendLine();
        sb.AppendLine("── JIT 编译 ──");
        sb.AppendLine($"  编译次数: {_jit_compilation_count}");
        sb.AppendLine($"  编译耗时: {jit_total_compilation_time_ms:F2}ms");
        sb.AppendLine($"  JIT 执行: {_jit_execution_count:N0}");
        sb.AppendLine($"  IC 命中率: {ic_hit_rate:P1} ({_ic_hit_count:N0}/{_ic_hit_count + _ic_miss_count:N0})");

        sb.AppendLine();
        sb.AppendLine("── 垃圾回收 ──");
        sb.AppendLine($"  Major GC: {_gc_major_collections}");
        sb.AppendLine($"  Minor GC: {_gc_minor_collections}");
        sb.AppendLine($"  总暂停: {gc_total_pause_time_ms:F2}ms");
        sb.AppendLine($"  最后暂停: {gc_last_pause_time_ms:F2}ms");
        sb.AppendLine($"  存活对象: {gc_live_object_count:N0}");
        sb.AppendLine($"  回收对象: {_gc_total_reclaimed:N0}");
        sb.AppendLine($"  晋升对象: {_gc_promoted_objects:N0}");

        sb.AppendLine();
        sb.AppendLine("── OSR ──");
        sb.AppendLine($"  编译: {_osr_compilation_count}");
        sb.AppendLine($"  迁移: {_osr_transition_count}");

        sb.AppendLine();
        sb.AppendLine("── FFI ──");
        sb.AppendLine($"  Intrinsic 调用: {_intrinsic_call_count:N0}");
        sb.AppendLine($"  Native 调用: {_native_call_count:N0}");

        return sb.ToString();
    }

    /// <summary>
    ///     重置所有指标
    /// </summary>
    public void reset()
    {
        _total_instructions = 0;
        _total_function_calls = 0;
        _execution_stopwatch.Reset();

        _jit_compilation_count = 0;
        jit_total_compilation_time_ms = 0;
        _jit_execution_count = 0;
        _ic_hit_count = 0;
        _ic_miss_count = 0;

        _gc_major_collections = 0;
        _gc_minor_collections = 0;
        gc_total_pause_time_ms = 0;
        gc_last_pause_time_ms = 0;
        gc_live_object_count = 0;
        _gc_total_reclaimed = 0;
        _gc_promoted_objects = 0;

        _osr_compilation_count = 0;
        _osr_transition_count = 0;

        _intrinsic_call_count = 0;
        _native_call_count = 0;
    }

    #endregion
}