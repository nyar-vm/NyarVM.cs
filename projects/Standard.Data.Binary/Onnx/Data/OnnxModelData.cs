namespace Std.Data.Binary.Onnx.Data;

/// <summary>
///     ONNX 数据类型枚举，对的onnx.TensorProto.DataType的
/// </summary>
public enum OnnxDataType
{
    undefined = 0,
    @float = 1,
    u_int8 = 2,
    int8 = 3,
    u_int16 = 4,
    int16 = 5,
    int32 = 6,
    int64 = 7,
    @string = 8,
    @bool = 9,
    float16 = 10,
    @double = 11,
    u_int32 = 12,
    u_int64 = 13,
    complex64 = 14,
    complex128 = 15,
    b_float16 = 16,
    float8_e4_m3_fn = 17,
    float8_e4_m3_fnuz = 18,
    float8_e5_m2 = 19,
    float8_e5_m2_fnuz = 20
}

/// <summary>
///     ONNX 张量数据，对的onnx.TensorProto的
/// </summary>
public sealed class OnnxTensor
{
    public string name { get; init; } = string.Empty;
    public OnnxDataType data_type { get; init; }
    public IReadOnlyList<long> dims { get; init; } = [];
    public byte[]? raw_data { get; init; }
    public IReadOnlyList<float> float_data { get; init; } = [];
    public IReadOnlyList<int> int32_data { get; init; } = [];
    public IReadOnlyList<long> int64_data { get; init; } = [];
    public IReadOnlyList<double> double_data { get; init; } = [];
}

/// <summary>
///     ONNX 节点属性值的
/// </summary>
public sealed class OnnxAttribute
{
    public string name { get; init; } = string.Empty;
    public OnnxAttributeType type { get; init; }
    public float float_value { get; init; }
    public int int_value { get; init; }
    public string string_value { get; init; } = string.Empty;
    public OnnxTensor? tensor_value { get; init; }
    public IReadOnlyList<float> floats { get; init; } = [];
    public IReadOnlyList<int> ints { get; init; } = [];
    public IReadOnlyList<string> strings { get; init; } = [];
}

/// <summary>
///     ONNX 属性类型的
/// </summary>
public enum OnnxAttributeType
{
    undefined = 0,
    @float = 1,
    @int = 2,
    @string = 3,
    tensor = 4,
    graph = 5,
    floats = 6,
    ints = 7,
    strings = 8,
    tensors = 9,
    graphs = 10
}

/// <summary>
///     ONNX 计算节点，对的onnx.NodeProto的
/// </summary>
public sealed class OnnxNode
{
    public string name { get; init; } = string.Empty;
    public string op_type { get; init; } = string.Empty;
    public string domain { get; init; } = string.Empty;
    public string doc_string { get; init; } = string.Empty;
    public IReadOnlyList<string> input { get; init; } = [];
    public IReadOnlyList<string> output { get; init; } = [];
    public IReadOnlyList<OnnxAttribute> attribute { get; init; } = [];
}

/// <summary>
///     ONNX 值信息，对应 onnx.ValueInfoProto的
/// </summary>
public sealed class OnnxValueInfo
{
    public string name { get; init; } = string.Empty;
    public OnnxDataType data_type { get; init; }
    public IReadOnlyList<long> shape { get; init; } = [];
}

/// <summary>
///     ONNX 计算图，对应 onnx.GraphProto的
/// </summary>
public sealed class OnnxGraph
{
    public string name { get; init; } = string.Empty;
    public string doc_string { get; init; } = string.Empty;
    public IReadOnlyList<OnnxNode> node { get; init; } = [];
    public IReadOnlyList<OnnxValueInfo> input { get; init; } = [];
    public IReadOnlyList<OnnxValueInfo> output { get; init; } = [];
    public IReadOnlyList<OnnxValueInfo> value_info { get; init; } = [];
    public IReadOnlyList<OnnxTensor> initialization { get; init; } = [];
}

/// <summary>
///     ONNX 算子集标识，对应 onnx.OperatorSetIdProto的
/// </summary>
public sealed class OnnxOperatorSetId
{
    public string domain { get; init; } = string.Empty;
    public long version { get; init; }
}

/// <summary>
///     ONNX 模型数据，对的onnx.ModelProto的
/// </summary>
public sealed class OnnxModelData
{
    public long ir_version { get; init; }
    public string producer_name { get; init; } = string.Empty;
    public string producer_version { get; init; } = string.Empty;
    public string domain { get; init; } = string.Empty;
    public long model_version { get; init; }
    public string doc_string { get; init; } = string.Empty;
    public OnnxGraph? graph { get; init; }
    public IReadOnlyList<OnnxOperatorSetId> opset_import { get; init; } = [];
    public IReadOnlyList<string> custom_metadata { get; init; } = [];
}