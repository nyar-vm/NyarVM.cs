namespace Std.Command;

/// <summary>
///     命令行参数解析失败时抛出的异常，包含所有解析错误详情�?///
/// </summary>
public sealed class CommandParseException : Exception
{
    /// <summary>
    ///     初始�?<see cref="CommandParseException" /> 的新实例�?    ///
    /// </summary>
    /// <param name="errors">解析错误列表�?/param>
    public CommandParseException(IReadOnlyList<string> errors)
        : base($"命令行参数解析失败：{string.Join("; ", errors)}")
    {
        this.errors = errors;
    }

    /// <summary>
    ///     初始�?<see cref="CommandParseException" /> 的新实例，包含单个错误信息�?    ///
    /// </summary>
    /// <param name="error">解析错误信息�?/param>
    public CommandParseException(string error)
        : base($"命令行参数解析失败：{error}")
    {
        errors = [error];
    }

    /// <summary>
    ///     获取解析过程中收集的所有错误信息�?    ///
    /// </summary>
    public IReadOnlyList<string> errors { get; }
}