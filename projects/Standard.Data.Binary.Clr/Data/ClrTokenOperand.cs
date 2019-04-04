namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     元数据令牌操作数的
/// </summary>
public sealed class ClrTokenOperand : ClrOperand
{
    public override ClrOperandKind kind => ClrOperandKind.token;
    public uint value { get; set; }
}