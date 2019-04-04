namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     操作数类型的
/// </summary>
public enum ClrOperandKind
{
    none,
    int8,
    int16,
    int32,
    int64,
    float32,
    float64,
    @string,
    token,
    branch_target8,
    branch_target32,
    switch_targets,
    local_index,
    argument_index
}