namespace Std.Command;

/// <summary>
///     命令行解析结果
/// </summary>
/// <typeparam name="T">命令模型类型</typeparam>
public sealed class ParseResult<T> where T : new()
{
    /// <summary>
    ///     是否解析成功
    /// </summary>
    public bool success { get; set; }

    /// <summary>
    ///     解析后的模型实例
    /// </summary>
    public T? model { get; set; }

    /// <summary>
    ///     子命令名（如果路由到子命令）
    /// </summary>
    public string sub_command_name { get; set; } = string.Empty;

    /// <summary>
    ///     错误信息
    /// </summary>
    public string error_message { get; set; } = string.Empty;
}