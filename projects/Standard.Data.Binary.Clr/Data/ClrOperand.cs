namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     MSIL 操作数的
/// </summary>
public abstract class ClrOperand
{
    /// <summary>
    ///     操作数类型的
    /// </summary>
    public abstract ClrOperandKind kind { get; }
}