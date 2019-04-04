namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     异常处理类型（ECMA-335 §25.4.6）的
/// </summary>
public enum ClrExceptionHandlerKind : uint
{
    @catch = 0x0000,
    filter = 0x0001,
    @finally = 0x0002,
    fault = 0x0004
}