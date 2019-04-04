using Std.Binary.Attributes;
using Std.Codec;

namespace Std.Data.Binary.Wasm.Data;

/// <summary>
///     WebAssembly 二进制格式常量的
/// </summary>
/// <remarks>
///     所有常量值均来自 WebAssembly MVP 规范，Acorn 独占二进制编解码职责�?
/// </remarks>
public static class WasmConstants
{
    /// <summary>
    ///     Wasm MVP 版本号的
    /// </summary>
    public const uint version = 1;

    /// <summary>
    ///     函数类型标记字节�?
    /// </summary>
    public const byte function_type_form = 0x60;

    /// <summary>
    ///     Limits 无上限标志的
    /// </summary>
    public const byte limits_has_only_min = 0x00;

    /// <summary>
    ///     Limits 有上限标志的
    /// </summary>
    public const byte limits_has_min_max = 0x01;

    /// <summary>
    ///     全局变量不可变标志的
    /// </summary>
    public const byte global_immutable = 0x00;

    /// <summary>
    ///     全局变量可变标志�?
    /// </summary>
    public const byte global_mutable = 0x01;

    /// <summary>
    ///     递归类型组标记字节的
    /// </summary>
    public const byte rec_type_form = 0x4E;

    /// <summary>
    ///     子类型标记字节（非 final，可被继承）的
    /// </summary>
    public const byte sub_type_form = 0x50;

    /// <summary>
    ///     子类型标记字节（final，不可被继承）的
    /// </summary>
    public const byte sub_final_type = 0x4F;

    /// <summary>
    ///     复合类型结构体标记字节的
    /// </summary>
    public const byte struct_type_form = 0x5F;

    /// <summary>
    ///     复合类型数组标记字节的
    /// </summary>
    public const byte array_type_form = 0x5E;

    /// <summary>
    ///     字段不可变标记字节的
    /// </summary>
    public const byte field_immutable = 0x00;

    /// <summary>
    ///     字段可变标记字节�?
    /// </summary>
    public const byte field_mutable = 0x01;

    /// <summary>
    ///     Component Model 版本号的
    /// </summary>
    public const uint component_version = 0x0A;

    /// <summary>
    ///     JSON 填充字节（空格）�?
    /// </summary>
    public const byte padding_byte = 0x20;

    /// <summary>
    ///     Wasm 魔数（\0asm）的
    /// </summary>
    public static ReadOnlySpan<byte> magic_number => "\0asm"u8;
}

/// <summary>
///     WebAssembly 的ID 枚举�?
/// </summary>
public enum WasmSectionId : byte
{
    /// <summary>
    ///     自定义段�?
    /// </summary>
    custom = 0,

    /// <summary>
    ///     类型段的
    /// </summary>
    type = 1,

    /// <summary>
    ///     导入段的
    /// </summary>
    import = 2,

    /// <summary>
    ///     函数段的
    /// </summary>
    function = 3,

    /// <summary>
    ///     表段�?
    /// </summary>
    table = 4,

    /// <summary>
    ///     内存段的
    /// </summary>
    memory = 5,

    /// <summary>
    ///     全局段的
    /// </summary>
    global = 6,

    /// <summary>
    ///     导出段的
    /// </summary>
    export = 7,

    /// <summary>
    ///     起始段的
    /// </summary>
    start = 8,

    /// <summary>
    ///     元素段的
    /// </summary>
    element = 9,

    /// <summary>
    ///     代码段的
    /// </summary>
    code = 10,

    /// <summary>
    ///     数据段的
    /// </summary>
    data = 11,

    /// <summary>
    ///     数据计数段（WebAssembly 2.0+）的
    /// </summary>
    data_count = 12
}

/// <summary>
///     WebAssembly 初始化表达式操作码枚举的
/// </summary>
public enum WasmInitOpCode : byte
{
    /// <summary>
    ///     i32.const 指令�?
    /// </summary>
    i32_const = 0x41,

    /// <summary>
    ///     i64.const 指令�?
    /// </summary>
    i64_const = 0x42,

    /// <summary>
    ///     f32.const 指令�?
    /// </summary>
    f32_const = 0x43,

    /// <summary>
    ///     f64.const 指令�?
    /// </summary>
    f64_const = 0x44,

    /// <summary>
    ///     global.get 指令�?
    /// </summary>
    global_get = 0x23,

    /// <summary>
    ///     end 指令�?
    /// </summary>
    end = 0x0B
}

/// <summary>
///     Wasm 文件头部�?字节）的
/// </summary>
[BinarySerializable]
public struct WasmHeader
{
    [Field(order = 0, length = 4)] public FixedBytes4 magic;

    [Field(order = 1)] public uint version;
}