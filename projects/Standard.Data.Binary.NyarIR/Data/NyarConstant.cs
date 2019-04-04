namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     Nyar 常量池条目的
/// </summary>
public sealed class NyarConstant
{
    /// <summary>
    /// </summary>
    /// <param name="kind">类型</param>
    /// <param name="payload">载荷</param>
    public NyarConstant(NyarConstantKind kind, object? payload)
    {
        this.kind = kind;
        this.payload = payload;
    }

    /// <summary>
    ///     常量类型的
    /// </summary>
    public NyarConstantKind kind { get; init; }

    /// <summary>
    ///     常量值的
    /// </summary>
    public object? payload { get; init; }
}