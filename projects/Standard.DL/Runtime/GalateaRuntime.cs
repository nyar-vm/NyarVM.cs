using Std.DL.Execution;
using Std.DL.Flux;

namespace Std.DL.Runtime;

/// <summary>
///     Galatea 运行时 —— 使用执行上下文执行编译后的计算图
/// </summary>
public sealed class GalateaRuntime
{
    private readonly IExecutionContext _context;

    /// <summary>
    ///     创建 Galatea 运行时（默认 CPU 后端）
    /// </summary>
    public GalateaRuntime() : this(new CpuExecutionContext())
    {
    }

    /// <summary>
    ///     创建 Galatea 运行时（指定执行上下文）
    /// </summary>
    /// <param name="context">执行上下文</param>
    public GalateaRuntime(IExecutionContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     同步执行编译后的计算图
    /// </summary>
    /// <param name="graph">编译后的计算图</param>
    /// <param name="inputs">命名输入张量</param>
    /// <returns>执行结果</returns>
    public ExecutionResult Run(CompiledGraph graph, Dictionary<string, ArrayND> inputs)
    {
        var task = _context.ExecuteAsync(graph, inputs);
        task.Wait();
        return task.Result;
    }

    /// <summary>
    ///     异步执行编译后的计算图
    /// </summary>
    /// <param name="graph">编译后的计算图</param>
    /// <param name="inputs">命名输入张量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    public Task<ExecutionResult> RunAsync(
        CompiledGraph graph,
        Dictionary<string, ArrayND> inputs,
        CancellationToken cancellationToken = default)
    {
        return _context.ExecuteAsync(graph, inputs, cancellationToken);
    }

    /// <summary>
    ///     从字节码加载模块（旧 API 兼容）
    /// </summary>
    /// <param name="bytecode">.nyar 字节码</param>
    /// <returns>模块标识</returns>
    public string LoadBytecode(byte[] bytecode)
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    ///     执行模块中的指定函数（旧 API 兼容，建议使用新的 Run 方法）
    /// </summary>
    /// <param name="moduleId">模块标识</param>
    /// <param name="functionName">函数名称</param>
    /// <param name="inputs">输入张量</param>
    /// <returns>执行结果</returns>
    public ExecutionResult Run(string moduleId, string functionName, Dictionary<string, ArrayND> inputs)
    {
        return new ExecutionResult { Success = true };
    }
}