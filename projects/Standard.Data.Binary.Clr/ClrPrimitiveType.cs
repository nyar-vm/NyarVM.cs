namespace Std.Data.Binary.Clr;

/// <summary>
///     CLR 原始类型，表示内置的基本类型
/// </summary>
public sealed class ClrPrimitiveType : ClrType
{
    /// <summary>
    ///     创建原始类型引用
    /// </summary>
    /// <param name="kind">原始类型种类。</param>
    private ClrPrimitiveType(PrimitiveKind kind)
    {
        this.kind = kind;
    }

    /// <summary>
    ///     原始类型种类
    /// </summary>
    public PrimitiveKind kind { get; }

    /// <summary>
    ///     Void 类型
    /// </summary>
    public static ClrPrimitiveType @void { get; } = new(PrimitiveKind.@void);

    /// <summary>
    ///     Boolean 类型
    /// </summary>
    public static ClrPrimitiveType boolean { get; } = new(PrimitiveKind.boolean);

    /// <summary>
    ///     Int32 类型
    /// </summary>
    public static ClrPrimitiveType int32 { get; } = new(PrimitiveKind.int32);

    /// <summary>
    ///     Int64 类型
    /// </summary>
    public static ClrPrimitiveType int64 { get; } = new(PrimitiveKind.int64);

    /// <summary>
    ///     Float32 类型
    /// </summary>
    public static ClrPrimitiveType float32 { get; } = new(PrimitiveKind.float32);

    /// <summary>
    ///     Float64 类型
    /// </summary>
    public static ClrPrimitiveType float64 { get; } = new(PrimitiveKind.float64);

    /// <summary>
    ///     String 类型
    /// </summary>
    public static ClrPrimitiveType @string { get; } = new(PrimitiveKind.@string);

    /// <summary>
    ///     Object 类型
    /// </summary>
    public static ClrPrimitiveType @object { get; } = new(PrimitiveKind.@object);
}