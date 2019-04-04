namespace Nyar.VM.NyarVM;

/// <summary>
///     运行时资源限制配置，防止恶意字节码耗尽系统资源
/// </summary>
public sealed class ResourceLimits
{
    #region 常量

    /// <summary>
    ///     默认最大帧栈深度
    /// </summary>
    public const int default_max_frame_depth = 512;

    /// <summary>
    ///     默认最大值栈深度
    /// </summary>
    public const int default_max_stack_depth = 1024;

    /// <summary>
    ///     默认最大指令执行数（0 表示无限制）
    /// </summary>
    public const long default_max_instructions = 0;

    /// <summary>
    ///     默认最大对象分配数（0 表示无限制）
    /// </summary>
    public const int default_max_allocations = 0;

    /// <summary>
    ///     默认最大执行时间（毫秒，0 表示无限制）
    /// </summary>
    public const double default_max_execution_time_ms = 0;

    #endregion

    #region 属性

    /// <summary>
    ///     最大帧栈深度（调用深度限制）
    /// </summary>
    public int max_frame_depth { get; init; } = default_max_frame_depth;

    /// <summary>
    ///     最大值栈深度
    /// </summary>
    public int max_stack_depth { get; init; } = default_max_stack_depth;

    /// <summary>
    ///     最大指令执行数（0 表示无限制）
    /// </summary>
    public long max_instructions { get; init; } = default_max_instructions;

    /// <summary>
    ///     最大对象分配数（0 表示无限制）
    /// </summary>
    public int max_allocations { get; init; } = default_max_allocations;

    /// <summary>
    ///     最大执行时间（毫秒，0 表示无限制）
    /// </summary>
    public double max_execution_time_ms { get; init; } = default_max_execution_time_ms;

    #endregion

    #region 静态实例

    /// <summary>
    ///     默认资源限制（无限制）
    /// </summary>
    public static ResourceLimits @default { get; } = new();

    /// <summary>
    ///     严格资源限制（适用于不可信字节码）
    /// </summary>
    public static ResourceLimits strict { get; } = new()
    {
        max_frame_depth = 128,
        max_stack_depth = 256,
        max_instructions = 10_000_000,
        max_allocations = 100_000,
        max_execution_time_ms = 5000
    };

    /// <summary>
    ///     沙箱资源限制（适用于完全不可信字节码）
    /// </summary>
    public static ResourceLimits sandbox { get; } = new()
    {
        max_frame_depth = 64,
        max_stack_depth = 128,
        max_instructions = 1_000_000,
        max_allocations = 10_000,
        max_execution_time_ms = 1000
    };

    #endregion
}

/// <summary>
///     资源耗尽异常
/// </summary>
public sealed class ResourceLimitExceededException : Exception
{
    /// <summary>
    ///     初始化 ResourceLimitExceededException
    /// </summary>
    /// <param name="resourceType">超出限制的资源类型。</param>
    /// <param name="current">当前值。</param>
    /// <param name="limit">限制值。</param>
    public ResourceLimitExceededException(string resourceType, long current, long limit)
        : base($"资源耗尽：{resourceType} 当前值 {current} 超过限制 {limit}")
    {
        resource_type = resourceType;
        current_value = current;
        limit_value = limit;
    }

    /// <summary>
    ///     超出限制的资源类型
    /// </summary>
    public string resource_type { get; }

    /// <summary>
    ///     当前值
    /// </summary>
    public long current_value { get; }

    /// <summary>
    ///     限制值
    /// </summary>
    public long limit_value { get; }
}