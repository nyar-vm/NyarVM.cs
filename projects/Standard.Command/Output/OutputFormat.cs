namespace Std.Command.Output;

/// <summary>
///     输出格式枚举
/// </summary>
public enum OutputFormat
{
    /// <summary>
    ///     自动检测（终端→表格，管道→纯文本）
    /// </summary>
    auto = -1,

    /// <summary>
    ///     纯文本格式
    /// </summary>
    plain = 0,

    /// <summary>
    ///     JSON 格式
    /// </summary>
    json = 1,

    /// <summary>
    ///     表格格式
    /// </summary>
    table = 2
}