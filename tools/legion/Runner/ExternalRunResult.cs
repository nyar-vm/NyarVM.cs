namespace Legion.CLI.Runner;

/// <summary>
///     外部 Runner 执行结果
/// </summary>
public sealed class ExternalRunResult
{
    /// <summary>
    ///     进程退出码
    /// </summary>
    public int exit_code { get; init; }

    /// <summary>
    ///     标准输出内容
    /// </summary>
    public string stdout { get; init; } = string.Empty;

    /// <summary>
    ///     标准错误输出内容
    /// </summary>
    public string stderr { get; init; } = string.Empty;

    /// <summary>
    ///     执行是否成功（退出码为 0）
    /// </summary>
    public bool success => exit_code == 0;
}