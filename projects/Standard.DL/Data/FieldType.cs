namespace Std.DL.Data;

/// <summary>
///     TypeCode 静态常量
/// </summary>
public static class FieldType
{
    /// <summary>有符号 8 位整数</summary>
    public const byte I8 = 1;

    /// <summary>无符号 8 位整数</summary>
    public const byte U8 = 2;

    /// <summary>有符号 16 位整数</summary>
    public const byte I16 = 3;

    /// <summary>无符号 16 位整数</summary>
    public const byte U16 = 4;

    /// <summary>有符号 32 位整数</summary>
    public const byte I32 = 5;

    /// <summary>无符号 32 位整数</summary>
    public const byte U32 = 6;

    /// <summary>有符号 64 位整数</summary>
    public const byte I64 = 7;

    /// <summary>无符号 64 位整数</summary>
    public const byte U64 = 8;

    /// <summary>半精度浮点 (float16)</summary>
    public const byte F16 = 9;

    /// <summary>单精度浮点 (float32)</summary>
    public const byte F32 = 10;

    /// <summary>双精度浮点 (float64)</summary>
    public const byte F64 = 11;

    /// <summary>Brain 浮点 (bfloat16)</summary>
    public const byte BF16 = 12;
}