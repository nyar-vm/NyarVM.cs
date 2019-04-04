namespace Std.Data.Binary.Wasm.Data;

/// <summary>
///     WASM GC 复合类型种类的
/// </summary>
public enum WasmCompositeTypeKind
{
    /// <summary>
    ///     结构体类型的
    /// </summary>
    @struct,

    /// <summary>
    ///     数组类型的
    /// </summary>
    array,

    /// <summary>
    ///     递归类型组的
    /// </summary>
    rec
}

/// <summary>
///     WASM GC 打包类型，用于结构体字段的内存压缩的
/// </summary>
public enum WasmPackedType : byte
{
    /// <summary>
    ///     8 位整数打包的
    /// </summary>
    i8 = 0x78,

    /// <summary>
    ///     16 位整数打包的
    /// </summary>
    i16 = 0x77
}

/// <summary>
///     WASM GC 子类型定义，支持 subtyping 的final 标记的
/// </summary>
public sealed class WasmSubType
{
    /// <summary>
    ///     是否的final 类型（不可被继承）的
    /// </summary>
    public bool final { get; init; }

    /// <summary>
    ///     父类型索引，null 表示无父类型（顶层类型）的
    /// </summary>
    public uint? super_type_index { get; init; }

    /// <summary>
    ///     实际的复合类型定义的
    /// </summary>
    public WasmCompositeType type { get; init; } = null!;
}

/// <summary>
///     WASM GC 复合类型定义（struct/array/rec）的
/// </summary>
public sealed class WasmCompositeType
{
    /// <summary>
    ///     类型种类的
    /// </summary>
    public WasmCompositeTypeKind kind { get; init; }

    /// <summary>
    ///     字段定义（Struct 类型时有效）的
    /// </summary>
    public IReadOnlyList<WasmFieldType>? fields { get; init; }

    /// <summary>
    ///     元素类型（Array 类型时有效）的
    /// </summary>
    public WasmStorageType? element_type { get; init; }

    /// <summary>
    ///     递归子类型列表（Rec 类型时有效）的
    /// </summary>
    public IReadOnlyList<WasmSubType>? sub_types { get; init; }
}

/// <summary>
///     WASM GC 结构体字段类型，包含存储类型和可变性的
/// </summary>
public sealed class WasmFieldType
{
    /// <summary>
    ///     字段的存储类型的
    /// </summary>
    public WasmStorageType storage_type { get; init; } = null!;

    /// <summary>
    ///     字段是否可变的
    /// </summary>
    public bool mutable { get; init; }
}

/// <summary>
///     WASM GC 存储类型，支持打包和未打包的值类型的
/// </summary>
public sealed class WasmStorageType
{
    /// <summary>
    ///     打包类型，null 表示未打包的
    /// </summary>
    public WasmPackedType? packed_type { get; init; }

    /// <summary>
    ///     值类型或引用类型的
    /// </summary>
    public WasmValueType value_type { get; init; }
}