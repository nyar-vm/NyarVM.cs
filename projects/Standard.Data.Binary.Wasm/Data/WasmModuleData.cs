namespace Std.Data.Binary.Wasm.Data;

/// <summary>
///     WebAssembly 模块数据，包含完整的 Wasm 二进制模块结构的
/// </summary>
public sealed class WasmModuleData
{
    /// <summary>
    ///     Wasm 二进制格式版本号的
    /// </summary>
    public uint version { get; init; } = 1;

    /// <summary>
    ///     类型段，包含所有函数签名定义的
    /// </summary>
    public IReadOnlyList<WasmFunctionType> types { get; init; } = [];

    /// <summary>
    ///     GC 子类型定义列表（WASM GC 提案）的
    ///     null 表示不发的GC 类型段，兼容 MVP 模式的
    /// </summary>
    public IReadOnlyList<WasmSubType>? gc_sub_types { get; set; }

    /// <summary>
    ///     导入段，包含所有外部导入项的
    /// </summary>
    public IReadOnlyList<WasmImport> imports { get; init; } = [];

    /// <summary>
    ///     函数段，包含每个函数的类型索引（不含导入函数）的
    /// </summary>
    public IReadOnlyList<uint> function_type_indices { get; init; } = [];

    /// <summary>
    ///     表段，包含所有表定义的
    /// </summary>
    public IReadOnlyList<WasmTable> tables { get; init; } = [];

    /// <summary>
    ///     内存段，包含所有内存定义的
    /// </summary>
    public IReadOnlyList<WasmMemory> memories { get; init; } = [];

    /// <summary>
    ///     全局段，包含所有全局变量定义的
    /// </summary>
    public IReadOnlyList<WasmGlobal> globals { get; init; } = [];

    /// <summary>
    ///     导出段，包含所有导出项的
    /// </summary>
    public IReadOnlyList<WasmExport> exports { get; init; } = [];

    /// <summary>
    ///     起始函数索引的
    /// </summary>
    public uint? start_function_index { get; init; }

    /// <summary>
    ///     元素段，包含表初始化数据的
    /// </summary>
    public IReadOnlyList<WasmElement> elements { get; init; } = [];

    /// <summary>
    ///     代码段，包含函数体（不含导入函数）的
    /// </summary>
    public IReadOnlyList<WasmCode> codes { get; init; } = [];

    /// <summary>
    ///     数据段，包含内存初始化数据的
    /// </summary>
    public IReadOnlyList<WasmData> data_segments { get; init; } = [];

    /// <summary>
    ///     标签段，包含异常处理标签定义（Exception Handling 提案）的
    /// </summary>
    public IReadOnlyList<WasmTag> tags { get; init; } = [];

    /// <summary>
    ///     自定义段列表的
    /// </summary>
    public IReadOnlyList<WasmCustomSection> custom_sections { get; init; } = [];
}

/// <summary>
///     Wasm 值类型的
/// </summary>
public enum WasmValueType : byte
{
    /// <summary>
    ///     32 位整数的
    /// </summary>
    int32 = 0x7F,

    /// <summary>
    ///     64 位整数的
    /// </summary>
    int64 = 0x7E,

    /// <summary>
    ///     32 位浮点数的
    /// </summary>
    float32 = 0x7D,

    /// <summary>
    ///     64 位浮点数的
    /// </summary>
    float64 = 0x7C,

    /// <summary>
    ///     函数引用的
    /// </summary>
    func_ref = 0x70,

    /// <summary>
    ///     外部引用的
    /// </summary>
    extern_ref = 0x6F,

    /// <summary>
    ///     任意 GC 对象引用的
    /// </summary>
    any_ref = 0x6E,

    /// <summary>
    ///     支持相等比较的GC 对象引用的
    /// </summary>
    eq_ref = 0x6D,

    /// <summary>
    ///     31 位整数引用的
    /// </summary>
    i31_ref = 0x6C,

    /// <summary>
    ///     结构体引用的
    /// </summary>
    struct_ref = 0x6B,

    /// <summary>
    ///     数组引用的
    /// </summary>
    array_ref = 0x6A,

    /// <summary>
    ///     空引用的
    /// </summary>
    null_ref = 0x69,

    /// <summary>
    ///     空函数引用的
    /// </summary>
    null_func_ref = 0x68,

    /// <summary>
    ///     空外部引用的
    /// </summary>
    null_extern_ref = 0x67,

    /// <summary>
    ///     128 的SIMD 向量的
    /// </summary>
    v128 = 0x7B
}

/// <summary>
///     Wasm 函数类型（函数签名）的
/// </summary>
public sealed class WasmFunctionType
{
    /// <summary>
    ///     参数类型列表的
    /// </summary>
    public IReadOnlyList<WasmValueType> parameters { get; init; } = [];

    /// <summary>
    ///     返回值类型列表的
    /// </summary>
    public IReadOnlyList<WasmValueType> results { get; init; } = [];
}

/// <summary>
///     Wasm 限制（用于表和内存的容量范围）的
/// </summary>
public sealed class WasmLimits
{
    /// <summary>
    ///     最小值的
    /// </summary>
    public uint minimum { get; init; }

    /// <summary>
    ///     最大值，null 表示无上限的
    /// </summary>
    public uint? maximum { get; init; }
}

/// <summary>
///     Wasm 表类型的
/// </summary>
public sealed class WasmTableType
{
    /// <summary>
    ///     元素类型的
    /// </summary>
    public WasmValueType element_type { get; init; } = WasmValueType.func_ref;

    /// <summary>
    ///     容量限制的
    /// </summary>
    public WasmLimits limits { get; init; } = new();
}

/// <summary>
///     Wasm 内存类型的
/// </summary>
public sealed class WasmMemoryType
{
    /// <summary>
    ///     容量限制的
    /// </summary>
    public WasmLimits limits { get; init; } = new();
}

/// <summary>
///     Wasm 全局类型的
/// </summary>
public sealed class WasmGlobalType
{
    /// <summary>
    ///     值类型的
    /// </summary>
    public WasmValueType value_type { get; init; }

    /// <summary>
    ///     是否可变的
    /// </summary>
    public bool mutable { get; init; }
}

/// <summary>
///     Wasm 导入描述符的
/// </summary>
public sealed class WasmImport
{
    /// <summary>
    ///     模块名称的
    /// </summary>
    public string module { get; init; } = string.Empty;

    /// <summary>
    ///     字段名称的
    /// </summary>
    public string field { get; init; } = string.Empty;

    /// <summary>
    ///     导入描述的
    /// </summary>
    public WasmImportDescriptor descriptor { get; init; } = new();
}

/// <summary>
///     Wasm 导入描述，包含导入类型和对应索引的
/// </summary>
public sealed class WasmImportDescriptor
{
    /// <summary>
    ///     导入种类的
    /// </summary>
    public WasmExternalKind kind { get; init; }

    /// <summary>
    ///     函数类型索引（当 Kind 的Function 时有效）的
    /// </summary>
    public uint function_type_index { get; init; }

    /// <summary>
    ///     表类型（的Kind 的Table 时有效）的
    /// </summary>
    public WasmTableType? table_type { get; init; }

    /// <summary>
    ///     内存类型（当 Kind 的Memory 时有效）的
    /// </summary>
    public WasmMemoryType? memory_type { get; init; }

    /// <summary>
    ///     全局类型（当 Kind 的Global 时有效）的
    /// </summary>
    public WasmGlobalType? global_type { get; init; }

    /// <summary>
    ///     标签类型（当 Kind 的Tag 时有效）的
    /// </summary>
    public WasmTagType? tag_type { get; init; }
}

/// <summary>
///     Wasm 外部种类的
/// </summary>
public enum WasmExternalKind : byte
{
    /// <summary>
    ///     函数的
    /// </summary>
    function = 0x00,

    /// <summary>
    ///     表的
    /// </summary>
    table = 0x01,

    /// <summary>
    ///     内存的
    /// </summary>
    memory = 0x02,

    /// <summary>
    ///     全局变量的
    /// </summary>
    global = 0x03,

    /// <summary>
    ///     异常标签（Exception Handling 提案）的
    /// </summary>
    tag = 0x04
}

/// <summary>
///     Wasm 表定义的
/// </summary>
public sealed class WasmTable
{
    /// <summary>
    ///     表类型的
    /// </summary>
    public WasmTableType type { get; init; } = new();
}

/// <summary>
///     Wasm 内存定义的
/// </summary>
public sealed class WasmMemory
{
    /// <summary>
    ///     内存类型的
    /// </summary>
    public WasmMemoryType type { get; init; } = new();
}

/// <summary>
///     Wasm 全局变量定义的
/// </summary>
public sealed class WasmGlobal
{
    /// <summary>
    ///     全局类型的
    /// </summary>
    public WasmGlobalType type { get; init; } = new();

    /// <summary>
    ///     初始化指令字节码的
    /// </summary>
    public byte[] init_expression { get; init; } = [];
}

/// <summary>
///     Wasm 导出项的
/// </summary>
public sealed class WasmExport
{
    /// <summary>
    ///     导出名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     导出种类的
    /// </summary>
    public WasmExternalKind kind { get; init; }

    /// <summary>
    ///     导出项索引的
    /// </summary>
    public uint index { get; init; }
}

/// <summary>
///     Wasm 元素段项（表初始化数据）的
/// </summary>
public sealed class WasmElement
{
    /// <summary>
    ///     目标表索引的
    /// </summary>
    public uint table_index { get; init; }

    /// <summary>
    ///     偏移量初始化指令字节码的
    /// </summary>
    public byte[] offset_expression { get; init; } = [];

    /// <summary>
    ///     初始化元素列表（函数索引）的
    /// </summary>
    public IReadOnlyList<uint> init_values { get; init; } = [];
}

/// <summary>
///     Wasm 代码段项（函数体）的
/// </summary>
public sealed class WasmCode
{
    /// <summary>
    ///     局部变量声明列表的
    /// </summary>
    public IReadOnlyList<WasmLocal> locals { get; init; } = [];

    /// <summary>
    ///     函数体字节码（不含局部变量声明）的
    /// </summary>
    public byte[] body { get; init; } = [];

    /// <summary>
    ///     函数体指令编码的总字节数（含 end 操作码），用于解码器预分配缓冲区的
    /// </summary>
    public uint max_length { get; init; }
}

/// <summary>
///     Wasm 局部变量声明的
/// </summary>
public sealed class WasmLocal
{
    /// <summary>
    ///     变量数量的
    /// </summary>
    public uint count { get; init; }

    /// <summary>
    ///     变量类型的
    /// </summary>
    public WasmValueType type { get; init; }
}

/// <summary>
///     Wasm 数据段项（内存初始化数据）的
/// </summary>
public sealed class WasmData
{
    /// <summary>
    ///     目标内存索引的
    /// </summary>
    public uint memory_index { get; init; }

    /// <summary>
    ///     偏移量初始化指令字节码的
    /// </summary>
    public byte[] offset_expression { get; init; } = [];

    /// <summary>
    ///     初始化数据的
    /// </summary>
    public byte[] initializer { get; init; } = [];
}

/// <summary>
///     Wasm 自定义段的
/// </summary>
public sealed class WasmCustomSection
{
    /// <summary>
    ///     段名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     段数据的
    /// </summary>
    public byte[] data { get; init; } = [];
}

/// <summary>
///     Wasm 异常标签定义（Exception Handling 提案）的
/// </summary>
public sealed class WasmTag
{
    /// <summary>
    ///     标签属性（0x00 表示异常标签）的
    /// </summary>
    public byte attribute { get; init; }

    /// <summary>
    ///     标签的函数类型索引（定义抛出的参数类型）的
    /// </summary>
    public uint type_index { get; init; }
}

/// <summary>
///     Wasm 异常标签类型，仅包含函数类型索引的
/// </summary>
public sealed class WasmTagType
{
    /// <summary>
    ///     标签对应的函数类型索引，定义异常抛出的参数类型的
    /// </summary>
    public uint function_type_index { get; init; }
}

/// <summary>
///     try_table 指令的catch 子句的种类（Exception Handling 提案）的
/// </summary>
public enum WasmCatchKind : byte
{
    /// <summary>
    ///     按标签索引捕获异常，不提取异常引用的
    /// </summary>
    @catch = 0x00,

    /// <summary>
    ///     按标签索引捕获异常，并将异常引用压入栈的
    /// </summary>
    catch_ref = 0x01,

    /// <summary>
    ///     捕获所有异常，不提取异常引用的
    /// </summary>
    catch_all = 0x02,

    /// <summary>
    ///     捕获所有异常，并将异常引用压入栈的
    /// </summary>
    catch_all_ref = 0x03
}