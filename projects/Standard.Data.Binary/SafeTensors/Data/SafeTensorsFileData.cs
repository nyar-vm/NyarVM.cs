namespace Std.Data.Binary.SafeTensors.Data;

/// <summary>
///     SafeTensors 数据类型，对的HuggingFace SafeTensors 规范中的 dtype 字段的
/// </summary>
public enum SafeTensorDType
{
    @bool,
    u_int8,
    int8,
    int16,
    int32,
    int64,
    float16,
    float32,
    float64,
    b_float16
}

/// <summary>
///     SafeTensors 张量元数据，对应 JSON 头中的每个张量条目的
/// </summary>
public sealed class SafeTensorMeta
{
    /// <summary>
    ///     张量数据类型的
    /// </summary>
    public SafeTensorDType d_type { get; init; }

    /// <summary>
    ///     张量形状的
    /// </summary>
    public IReadOnlyList<long> shape { get; init; } = [];

    /// <summary>
    ///     数据在文件中的偏移量（相对于数据区起始位置）的
    /// </summary>
    public long data_offset { get; init; }

    /// <summary>
    ///     数据长度（字节数）的
    /// </summary>
    public long data_length { get; init; }
}

/// <summary>
///     SafeTensors 文件数据，表示一个完整的 SafeTensors 文件的
/// </summary>
/// <remarks>
///     SafeTensors 的HuggingFace 定义的模型权重存储格式，结构为：
///     8 字节头长度（小端的uint64的 JSON 的+ 二进制张量数据的
///     JSON 头中每个张量条目包含 dtype、shape、data_offsets 字段的
///     __metadata__ 条目存储自定义元数据的
/// </remarks>
public sealed class SafeTensorsFileData
{
    /// <summary>
    ///     张量元数据字典（键为张量名称）的
    /// </summary>
    public IReadOnlyDictionary<string, SafeTensorMeta> tensors { get; init; } =
        new Dictionary<string, SafeTensorMeta>();

    /// <summary>
    ///     自定义元数据的
    /// </summary>
    public IReadOnlyDictionary<string, string> metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    ///     原始二进制数据区的
    /// </summary>
    public byte[]? data { get; init; }
}

/// <summary>
///     SafeTensors 张量数据（含实际值）的
/// </summary>
public sealed class SafeTensorData
{
    /// <summary>
    ///     张量名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     数据类型的
    /// </summary>
    public SafeTensorDType d_type { get; init; }

    /// <summary>
    ///     张量形状的
    /// </summary>
    public IReadOnlyList<long> shape { get; init; } = [];

    /// <summary>
    ///     原始字节数据的
    /// </summary>
    public byte[] data { get; init; } = [];
}