namespace Std.State;

/// <summary>
///     定时任务接口，所有 Cron 任务必须实现此接口。
/// </summary>
public interface ICronJob
{
    /// <summary>
    ///     任务名称（用于日志和仪表盘显示）。
    /// </summary>
    string name { get; }

    /// <summary>
    ///     Cron 表达式，定义任务的执行计划。
    /// </summary>
    /// <example>"0 * * * *" 表示每小时整点执行</example>
    string cron_expression { get; }

    /// <summary>
    ///     执行定时任务。
    /// </summary>
    /// <param name="cancellationToken">取消令牌，触发时任务应尽快停止</param>
    /// <returns>异步操作</returns>
    Task execute(CancellationToken cancellationToken = default);
}

/// <summary>
///     定时任务运行状态。
/// </summary>
public sealed class CronJobState
{
    /// <summary>
    ///     任务名称。
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     Cron 表达式。
    /// </summary>
    public string cron_expression { get; init; } = string.Empty;

    /// <summary>
    ///     是否为活动状态。
    /// </summary>
    public bool is_active { get; init; }

    /// <summary>
    ///     上次运行时间。
    /// </summary>
    public DateTimeOffset? last_run_time { get; init; }

    /// <summary>
    ///     上次运行是否成功。
    /// </summary>
    public bool? last_run_success { get; init; }

    /// <summary>
    ///     下次计划运行时间。
    /// </summary>
    public DateTimeOffset? next_run_time { get; init; }

    /// <summary>
    ///     总运行次数。
    /// </summary>
    public long run_count { get; init; }
}

/// <summary>
///     Cron 调度器接口，管理所有定时任务的注册和执行。
/// </summary>
public interface IScheduler
{
    /// <summary>
    ///     注册一个定时任务。
    /// </summary>
    /// <param name="job">定时任务实例</param>
    void register_job(ICronJob job);

    /// <summary>
    ///     启动调度器，开始按计划执行任务。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步操作</returns>
    Task start(CancellationToken cancellationToken = default);

    /// <summary>
    ///     停止调度器，等待进行中的任务完成。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步操作</returns>
    Task stop(CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取所有已注册任务的状态。
    /// </summary>
    /// <returns>任务状态列表</returns>
    IReadOnlyList<CronJobState> get_job_states();
}