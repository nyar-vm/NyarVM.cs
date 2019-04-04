namespace Legion.CLI.Runner;

/// <summary>
///     Runner 抽象接口，定义测试执行器的统一契约。
///     In-Process Runner（NyarVm）和 External Runner（CLR/JVM/Node）均实现此接口。
/// </summary>
public interface IRunner
{
    /// <summary>
    ///     检查当前 Runner 是否可用（外部命令在 PATH 上可找到）
    /// </summary>
    bool is_available();

    /// <summary>
    ///     执行编译产物
    /// </summary>
    /// <param name="artifactPath">产物路径</param>
    /// <param name="entryPoint">入口点名称（函数名或类名）</param>
    /// <returns>执行结果</returns>
    ExternalRunResult run(string artifactPath, string? entryPoint);
}