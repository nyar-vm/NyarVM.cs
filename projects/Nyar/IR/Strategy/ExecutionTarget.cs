namespace Nyar.IR.Strategy;

/// <summary>
///     执行调度目标
/// </summary>
public enum ExecutionTarget
{
    /// <summary>CPU 执行。</summary>
    cpu,

    /// <summary>GPU 执行。</summary>
    gpu,

    /// <summary>异步运行时执行。</summary>
    async,

    /// <summary>空间/硬件空间执行。</summary>
    spatial,

    /// <summary>编译期执行。</summary>
    comptime,

    /// <summary>资源管理域执行。</summary>
    resource,

    /// <summary>安全/沙箱域执行。</summary>
    safe
}