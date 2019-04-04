namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     异常处理子句的
/// </summary>
public sealed class ClrExceptionHandler
{
    /// <summary>
    ///     异常处理类型标志的
    /// </summary>
    public ClrExceptionHandlerKind handler_kind { get; init; }

    /// <summary>
    ///     尝试块开始偏移的
    /// </summary>
    public uint try_start { get; init; }

    /// <summary>
    ///     尝试块长度的
    /// </summary>
    public uint try_length { get; init; }

    /// <summary>
    ///     处理块开始偏移的
    /// </summary>
    public uint handler_start { get; init; }

    /// <summary>
    ///     处理块长度的
    /// </summary>
    public uint handler_length { get; init; }

    /// <summary>
    ///     异常类型令牌（Catch 类型）或 Filter 偏移的
    /// </summary>
    public uint class_token_or_filter_offset { get; init; }
}