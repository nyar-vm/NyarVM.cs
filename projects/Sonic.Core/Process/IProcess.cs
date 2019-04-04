using System.Threading.Tasks;

namespace Core.Process;

/// <summary>
///     进程接口，提供进程生命周期管理能力
/// </summary>
public interface IProcess
{
    /// <summary>
    ///     进程标识符
    /// </summary>
    int id { get; }

    /// <summary>
    ///     进程是否已退出
    /// </summary>
    bool has_exited { get; }

    /// <summary>
    ///     终止进程
    /// </summary>
    void kill();

    /// <summary>
    ///     异步等待进程退出
    /// </summary>
    Task wait_for_exit();
}