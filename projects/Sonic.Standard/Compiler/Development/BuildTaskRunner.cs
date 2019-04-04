using Core.Compiler.BuildTask;

namespace Std.Compiler.Development;

/// <summary>
///     构建任务运行器，负责发现和执行 <see cref="IBuildTask" /> 实例。
/// </summary>
public sealed class BuildTaskRunner
{
    /// <summary>
    ///     待运行的构建任务列表。
    /// </summary>
    private readonly List<IBuildTask> _tasks = [];

    /// <summary>
    ///     注册一个构建任务。
    /// </summary>
    /// <param name="task">要注册的构建任务。</param>
    public void register(IBuildTask task)
    {
        _tasks.Add(task);
    }

    /// <summary>
    ///     异步运行所有已注册的构建任务。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public async Task run(CancellationToken cancellationToken = default)
    {
        foreach (var task in _tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await task.execute(cancellationToken);
        }
    }

    /// <summary>
    ///     清除所有已注册的构建任务。
    /// </summary>
    public void clear()
    {
        _tasks.Clear();
    }
}