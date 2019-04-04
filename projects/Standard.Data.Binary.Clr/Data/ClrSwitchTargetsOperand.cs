namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     开关目标操作数的
/// </summary>
public sealed class ClrSwitchTargetsOperand : ClrOperand
{
    public override ClrOperandKind kind => ClrOperandKind.switch_targets;
    public IReadOnlyList<int> offsets { get; init; } = [];
}