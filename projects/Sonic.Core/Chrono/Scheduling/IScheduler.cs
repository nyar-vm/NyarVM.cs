using System.Threading.Tasks;

namespace Core.Chrono.Scheduling;

/// <summary>
///     任务调度器接口
/// </summary>
public interface IScheduler
{
    /// <summary>
    ///     使用 Cron 表达式调度后台任务
    /// </summary>
    /// <param name="job">后台任务</param>
    /// <param name="cron">Cron 表达式</param>
    Task schedule(IBackgroundJob job, string cron);
}