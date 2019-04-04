namespace Core.Chrono.Scheduling;

/// <summary>
///     周期性任务接口
/// </summary>
public interface IRecurringJob : IBackgroundJob
{
    /// <summary>
    ///     Cron 表达式
    /// </summary>
    string cron_expression { get; }
}