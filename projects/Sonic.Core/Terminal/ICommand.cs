using System.Threading.Tasks;

namespace Core.Terminal;

/// <summary>
///     定义命令执行接口。实现此接口的类型可通过 <see cref="execute" /> 执行命令逻辑并返回退出码。
/// </summary>
public interface ICommand
{
    /// <summary>
    ///     异步执行命令，返回进程退出码。
    /// </summary>
    /// <returns>进程退出码，0 表示成功，非零表示失败。</returns>
    Task<int> execute();
}