namespace Nyar.VM.NyarVM.Jit;

/// <summary>
///     JIT 编译器配置选项
/// </summary>
public sealed class JitOptions
{
    /// <summary>
    ///     是否启用 JIT 编译
    /// </summary>
    public bool enabled { get; init; } = true;

    /// <summary>
    ///     热点阈值：函数被调用多少次后触发 JIT 编译
    /// </summary>
    public int hot_threshold { get; init; } = 100;

    /// <summary>
    ///     最大 JIT 编译函数数量（防止内存膨胀）
    /// </summary>
    public int max_compiled_functions { get; init; } = 1024;

    /// <summary>
    ///     是否启用分层编译（先解释执行，热点后 JIT）
    /// </summary>
    public bool tiered_compilation { get; init; } = true;

    /// <summary>
    ///     JIT 编译超时时间（毫秒），0 表示无超时
    /// </summary>
    public int compilation_timeout_ms { get; init; } = 5000;
}