using Core.Process;

namespace Std.Process;

/// <summary>
///     进程运行器，实现 <see cref="IProcess" /> 接口，
///     封装 <see cref="System.Diagnostics.Process" /> 提供进程生命周期管理能力。
/// </summary>
public sealed class ProcessRunner : IProcess
{
    /// <summary>
    ///     内部系统进程对象。
    /// </summary>
    private readonly System.Diagnostics.Process _process;

    /// <summary>
    ///     初始化 <see cref="ProcessRunner" /> 的新实例。
    /// </summary>
    /// <param name="process">系统进程对象。</param>
    public ProcessRunner(System.Diagnostics.Process process)
    {
        _process = process;
    }

    /// <summary>
    ///     获取进程标识符。
    /// </summary>
    public int id => _process.Id;

    /// <summary>
    ///     获取进程是否已退出。
    /// </summary>
    public bool has_exited
    {
        get
        {
            try
            {
                return _process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <summary>
    ///     终止进程。
    /// </summary>
    public void kill()
    {
        _process.Kill();
    }

    /// <summary>
    ///     异步等待进程退出。
    /// </summary>
    /// <returns>异步任务。</returns>
    public async Task wait_for_exit()
    {
        await _process.WaitForExitAsync();
    }
}