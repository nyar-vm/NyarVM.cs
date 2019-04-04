using System.Threading.Tasks;

namespace Core.Chrono.Scheduling;

/// <summary>
///     后台任务接口
/// </summary>
public interface IBackgroundJob
{
    /// <summary>
    ///     异步执行任务
    /// </summary>
    Task execute();
}