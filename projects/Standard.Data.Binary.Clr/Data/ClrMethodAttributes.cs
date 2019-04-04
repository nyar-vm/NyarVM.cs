namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     方法访问标志（MethodAttributes，ECMA-335 §23.1.10）的
/// </summary>
[Flags]
public enum ClrMethodAttributes : ushort
{
    member_access_mask = 0x0007,
    @private = 0x0001,
    fam_and_assem = 0x0002,
    assembly = 0x0003,
    family = 0x0004,
    fam_or_assem = 0x0005,
    @public = 0x0006,
    @static = 0x0010,
    final = 0x0020,
    @virtual = 0x0040,
    hide_by_sig = 0x0080,
    vtable_layout_mask = 0x0100,
    reuse_slot = 0x0000,
    new_slot = 0x0100,
    @abstract = 0x0400,
    special_name = 0x0800,
    p_invoke_impl = 0x2000,
    unmanaged_export = 0x0008,
    rt_special_name = 0x1000,
    has_security = 0x4000,
    require_sec_object = 0x8000
}