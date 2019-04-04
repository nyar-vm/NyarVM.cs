namespace Core.Chrono.Scheduling;

/// <summary>
///     任务状态枚举
/// </summary>
public enum JobStatus
{
    /// <summary>
    ///     等待中
    /// </summary>
    pending,

    /// <summary>
    ///     运行中
    /// </summary>
    running,

    /// <summary>
    ///     已完成
    /// </summary>
    completed,

    /// <summary>
    ///     已失败
    /// </summary>
    failed,

    /// <summary>
    ///     已取消
    /// </summary>
    cancelled
}