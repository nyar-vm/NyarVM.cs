using Std.Binary.Attributes;
using Std.Codec;

namespace Std.Data.Binary.Dxil.Data;

/// <summary>
///     DXIL 程序头数据（24 字节）的
/// </summary>
/// <remarks>
///     DXIL 程序头位的DXIL Part 数据的起始位置，
///     包含着色器模型类型、DXIL 版本的LLVM Bitcode 的偏移与大小�?///
/// </remarks>
[BinarySerializable(endianness = Endianness.little_endian)]
public struct DxilProgramHeader
{
    /// <summary>
    ///     DXIL 主版本号�?
    /// </summary>
    [Field(order = 0)] public byte major_version;

    /// <summary>
    ///     DXIL 次版本号�?
    /// </summary>
    [Field(order = 1)] public byte minor_version;

    /// <summary>
    ///     着色器模型类型的see cref="DxilShaderModelKind" /> 枚举值）�?
    /// </summary>
    [Field(order = 2)] public byte shader_model_kind_raw;

    /// <summary>
    ///     着色器模型类型�?
    /// </summary>
    public DxilShaderModelKind shader_model_kind
    {
        get => (DxilShaderModelKind)shader_model_kind_raw;
        init => shader_model_kind_raw = (byte)value;
    }

    /// <summary>
    ///     对齐填充字节�?
    /// </summary>
    [Field(order = 3)] public byte padding;

    /// <summary>
    ///     DXIL 数据总大小（字节，含 ProgramHeader）的
    /// </summary>
    [Field(order = 4)] public uint size;

    /// <summary>
    ///     LLVM Bitcode 相对的DXIL Part 数据起始位置的偏移的
    /// </summary>
    [Field(order = 5)] public uint bitcode_offset;

    /// <summary>
    ///     LLVM Bitcode 大小（字节）�?
    /// </summary>
    [Field(order = 6)] public uint bitcode_size;
}

/// <summary>
///     DXIL 着色器特征标志�?///
/// </summary>
[Flags]
public enum DxilShaderFlags : ulong
{
    none = 0,

    /// <summary>
    ///     禁用着色器优化�?
    /// </summary>
    disable_optimizations = 1 << 0,

    /// <summary>
    ///     禁用数学重构�?
    /// </summary>
    disable_math_refactoring = 1 << 1,

    /// <summary>
    ///     着色器使用双精度浮点的
    /// </summary>
    uses_doubles = 1 << 2,

    /// <summary>
    ///     强制早期深度模板测试�?
    /// </summary>
    force_early_depth_stencil = 1 << 3,

    /// <summary>
    ///     启用原始和结构化缓冲区的
    /// </summary>
    enable_raw_and_structured_buffers = 1 << 4,

    /// <summary>
    ///     着色器使用半精度的
    /// </summary>
    uses_min_precision = 1 << 5,

    /// <summary>
    ///     着色器使用双精度扩展内联函数的
    /// </summary>
    uses_double_extensions = 1 << 6,

    /// <summary>
    ///     着色器使用 MSAD�?
    /// </summary>
    uses_msad = 1 << 7,

    /// <summary>
    ///     所有资源必须在着色器执行期间绑定�?
    /// </summary>
    all_resources_bound = 1 << 8,

    /// <summary>
    ///     着色器使用 Wave 操作内联函数�?
    /// </summary>
    uses_wave_ops = 1 << 19,

    /// <summary>
    ///     着色器使用 int64 指令�?
    /// </summary>
    uses_int64 = 1 << 20
}

/// <summary>
///     DXIL 着色器哈希数据�? 字节）的
/// </summary>
public sealed class DxilShaderHash
{
    /// <summary>
    ///     哈希标志�?= 包含源码信息�?= 不包含）�?
    /// </summary>
    public uint flags { get; init; }

    /// <summary>
    ///     SHA-1 哈希值（20 字节）的
    /// </summary>
    public byte[] digest { get; init; } = new byte[20];
}

/// <summary>
///     DXIL 着色器签名元素数据�?///
/// </summary>
public sealed class DxilSignatureElement
{
    /// <summary>
    ///     元素唯一 ID�?
    /// </summary>
    public int id { get; init; }

    /// <summary>
    ///     语义名称�?
    /// </summary>
    public string semantic_name { get; init; } = string.Empty;

    /// <summary>
    ///     组件类型�?
    /// </summary>
    public DxilComponentType component_type { get; init; }

    /// <summary>
    ///     语义种类�?
    /// </summary>
    public byte semantic_kind { get; init; }

    /// <summary>
    ///     语义索引列表�?
    /// </summary>
    public IReadOnlyList<int> semantic_indices { get; init; } = [];

    /// <summary>
    ///     插值模式的
    /// </summary>
    public DxilInterpolationMode interpolation_mode { get; init; }

    /// <summary>
    ///     行数�?
    /// </summary>
    public int rows { get; init; }

    /// <summary>
    ///     列数�?
    /// </summary>
    public byte columns { get; init; }

    /// <summary>
    ///     起始行的
    /// </summary>
    public int start_row { get; init; }

    /// <summary>
    ///     起始列的
    /// </summary>
    public byte start_column { get; init; }
}

/// <summary>
///     DXIL 着色器签名数据�?///
/// </summary>
public sealed class DxilSignature
{
    /// <summary>
    ///     签名元素列表�?
    /// </summary>
    public IReadOnlyList<DxilSignatureElement> elements { get; init; } = [];
}

/// <summary>
///     DXIL 资源记录数据�?///
/// </summary>
public sealed class DxilResourceRecord
{
    /// <summary>
    ///     资源唯一 ID�?
    /// </summary>
    public int id { get; init; }

    /// <summary>
    ///     资源名称�?
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     资源类的
    /// </summary>
    public DxilResourceClass resource_class { get; init; }

    /// <summary>
    ///     资源种类�?
    /// </summary>
    public DxilResourceKind resource_kind { get; init; }

    /// <summary>
    ///     绑定空间�?
    /// </summary>
    public int space_id { get; init; }

    /// <summary>
    ///     绑定下界�?
    /// </summary>
    public int lower_bound { get; init; }

    /// <summary>
    ///     绑定范围大小�?
    /// </summary>
    public int range_size { get; init; }
}

/// <summary>
///     DXIL 入口点数据的
/// </summary>
public sealed class DxilEntryPoint
{
    /// <summary>
    ///     入口点名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     着色器模型类型�?
    /// </summary>
    public DxilShaderModelKind shader_model { get; init; }

    /// <summary>
    ///     输入签名�?
    /// </summary>
    public DxilSignature? input_signature { get; init; }

    /// <summary>
    ///     输出签名�?
    /// </summary>
    public DxilSignature? output_signature { get; init; }

    /// <summary>
    ///     补丁常量签名�?
    /// </summary>
    public DxilSignature? patch_constant_signature { get; init; }

    /// <summary>
    ///     SRV 资源列表�?
    /// </summary>
    public IReadOnlyList<DxilResourceRecord> sr_vs { get; init; } = [];

    /// <summary>
    ///     UAV 资源列表�?
    /// </summary>
    public IReadOnlyList<DxilResourceRecord> ua_vs { get; init; } = [];

    /// <summary>
    ///     CBV 资源列表�?
    /// </summary>
    public IReadOnlyList<DxilResourceRecord> cb_vs { get; init; } = [];

    /// <summary>
    ///     采样器列表的
    /// </summary>
    public IReadOnlyList<DxilResourceRecord> samplers { get; init; } = [];
}

/// <summary>
///     DXIL 程序完整数据�?///
/// </summary>
public sealed class DxilProgramData
{
    /// <summary>
    ///     程序头的
    /// </summary>
    public DxilProgramHeader header { get; init; }

    /// <summary>
    ///     着色器特征标志�?
    /// </summary>
    public DxilShaderFlags shader_flags { get; init; }

    /// <summary>
    ///     入口点列表的
    /// </summary>
    public IReadOnlyList<DxilEntryPoint> entry_points { get; init; } = [];

    /// <summary>
    ///     LLVM Bitcode 原始数据�?
    /// </summary>
    public byte[] bitcode_data { get; init; } = [];
}