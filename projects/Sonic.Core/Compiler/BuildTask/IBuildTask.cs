using System.Threading;
using System.Threading.Tasks;

namespace Core.Compiler.BuildTask;

/// <summary>
///     构建任务接口，定义异步执行的契约
/// </summary>
public interface IBuildTask
{
    /// <summary>
    ///     异步执行构建任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task execute(CancellationToken cancellationToken);
}