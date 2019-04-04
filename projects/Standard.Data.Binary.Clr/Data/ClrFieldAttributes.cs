namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     字段访问标志（FieldAttributes，ECMA-335 §23.1.5）的
/// </summary>
[Flags]
public enum ClrFieldAttributes : ushort
{
    field_access_mask = 0x0007,
    @private = 0x0001,
    fam_and_assem = 0x0002,
    assembly = 0x0003,
    family = 0x0004,
    fam_or_assem = 0x0005,
    @public = 0x0006,
    @static = 0x0010,
    init_only = 0x0020,
    literal = 0x0040,
    not_serialized = 0x0080,
    special_name = 0x0200,
    p_invoke_impl = 0x2000,
    rt_special_name = 0x0400,
    has_field_marshal = 0x1000,
    has_default = 0x8000,
    has_field_rva = 0x0100
}