namespace Atlas.Cloud.Models;

/// <summary>
/// CDN 操作结果，包含操作状态和任务标识
/// </summary>
public sealed class CdnResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool success { get; init; }

    /// <summary>
    /// 错误消息，成功时为空
    /// </summary>
    public string? error { get; init; }

    /// <summary>
    /// 任务唯一标识，成功时由 CDN 平台返回
    /// </summary>
    public string? task_id { get; init; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="taskId">任务唯一标识</param>
    public static CdnResult ok(string? taskId = null) =>
        new() { success = true, task_id = taskId };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="error">错误消息</param>
    public static CdnResult fail(string error) =>
        new() { success = false, error = error };
}
