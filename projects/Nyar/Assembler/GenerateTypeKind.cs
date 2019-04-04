namespace Nyar.Assembler;

/// <summary>
///     元编译类型引用的分类枚举。
/// </summary>
public enum GenerateTypeKind : byte
{
    unknown_named,
    @void,
    unit,
    @bool,
    @char,
    i8,
    i16,
    i32,
    i64,
    i128,
    u8,
    u16,
    u32,
    u64,
    f32,
    f64,
    utf8,
    utf16,
    uuid,
    @object,
    any,
    @null,
    function_ref,
    external_ref,
    v128,
    unsafe_ref,
    self_ref,
    emptylist,
    bracket_array,
    array,
    list,
    map,
    set,
    user_defined_named
}