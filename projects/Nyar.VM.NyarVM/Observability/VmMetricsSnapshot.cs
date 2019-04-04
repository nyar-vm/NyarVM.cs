namespace Nyar.VM.NyarVM.Observability;

/// <summary>
///     VM 运行时指标快照，某一时刻的指标数据
/// </summary>
public sealed class VmMetricsSnapshot
{
    /// <summary>
    ///     累计执行指令数
    /// </summary>
    public long total_instructions { get; init; }

    /// <summary>
    ///     累计函数调用次数
    /// </summary>
    public long total_function_calls { get; init; }

    /// <summary>
    ///     累计执行时间（毫秒）
    /// </summary>
    public double total_execution_time_ms { get; init; }

    /// <summary>
    ///     JIT 编译次数
    /// </summary>
    public int jit_compilation_count { get; init; }

    /// <summary>
    ///     JIT 编译总耗时（毫秒）
    /// </summary>
    public double jit_total_compilation_time_ms { get; init; }

    /// <summary>
    ///     JIT 编译后函数执行次数
    /// </summary>
    public long jit_execution_count { get; init; }

    /// <summary>
    ///     内联缓存命中次数
    /// </summary>
    public long ic_hit_count { get; init; }

    /// <summary>
    ///     内联缓存未命中次数
    /// </summary>
    public long ic_miss_count { get; init; }

    /// <summary>
    ///     内联缓存命中率
    /// </summary>
    public double ic_hit_rate { get; init; }

    /// <summary>
    ///     Major GC 执行次数
    /// </summary>
    public long gc_major_collections { get; init; }

    /// <summary>
    ///     Minor GC 执行次数
    /// </summary>
    public long gc_minor_collections { get; init; }

    /// <summary>
    ///     GC 总暂停时间（毫秒）
    /// </summary>
    public double gc_total_pause_time_ms { get; init; }

    /// <summary>
    ///     GC 最后一次暂停时间（毫秒）
    /// </summary>
    public double gc_last_pause_time_ms { get; init; }

    /// <summary>
    ///     当前存活对象数
    /// </summary>
    public int gc_live_object_count { get; init; }

    /// <summary>
    ///     GC 总回收对象数
    /// </summary>
    public long gc_total_reclaimed { get; init; }

    /// <summary>
    ///     GC 晋升到老生代的对象总数
    /// </summary>
    public long gc_promoted_objects { get; init; }

    /// <summary>
    ///     OSR 编译次数
    /// </summary>
    public int osr_compilation_count { get; init; }

    /// <summary>
    ///     OSR 迁移次数
    /// </summary>
    public int osr_transition_count { get; init; }

    /// <summary>
    ///     Intrinsic 调用次数
    /// </summary>
    public long intrinsic_call_count { get; init; }

    /// <summary>
    ///     Native 调用次数
    /// </summary>
    public long native_call_count { get; init; }
}