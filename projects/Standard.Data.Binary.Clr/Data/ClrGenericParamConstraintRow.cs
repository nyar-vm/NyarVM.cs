namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     GenericParamConstraint 表行（ECMA-335 §22.21）的
/// </summary>
public sealed class ClrGenericParamConstraintRow
{
    public uint owner_index { get; init; }
    public uint constraint_index { get; init; }
}