namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     类型访问标志（TypeAttributes，ECMA-335 §23.1.14）的
/// </summary>
[Flags]
public enum ClrTypeAttributes : uint
{
    visibility_mask = 0x00000007,
    not_public = 0x00000000,
    @public = 0x00000001,
    nested_public = 0x00000002,
    nested_private = 0x00000003,
    nested_family = 0x00000004,
    nested_assembly = 0x00000005,
    nested_fam_and_assem = 0x00000006,
    nested_fam_or_assem = 0x00000007,
    sequential_layout = 0x00000008,
    explicit_layout = 0x00000010,
    @interface = 0x00000020,
    @abstract = 0x00000080,
    @sealed = 0x00000100,
    special_name = 0x00000400,
    rt_special_name = 0x00000800,
    import = 0x00001000,
    serializable = 0x00002000,
    string_format_mask = 0x00030000,
    ansi_class = 0x00000000,
    unicode_class = 0x00010000,
    auto_class = 0x00020000,
    custom_format_class = 0x00030000,
    before_field_init = 0x00100000,
    has_security = 0x00040000
}