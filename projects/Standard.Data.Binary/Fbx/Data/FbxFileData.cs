namespace Std.Data.Binary.Fbx.Data;

/// <summary>
///     FBX 文件数据的
/// </summary>
public sealed class FbxFileData
{
    /// <summary>
    ///     FBX 版本号的
    /// </summary>
    public int version { get; init; }

    /// <summary>
    ///     根节点的
    /// </summary>
    public FbxNode root { get; init; } = new();
}

/// <summary>
///     FBX 节点的
/// </summary>
public sealed class FbxNode
{
    /// <summary>
    ///     节点名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     属性列表的
    /// </summary>
    public IReadOnlyList<FbxProperty> properties { get; init; } = [];

    /// <summary>
    ///     子节点列表的
    /// </summary>
    public IReadOnlyList<FbxNode> children { get; init; } = [];
}

/// <summary>
///     FBX 属性的
/// </summary>
public sealed class FbxProperty
{
    /// <summary>
    ///     属性类型代码的
    /// </summary>
    public byte type_code { get; init; }

    /// <summary>
    ///     属性值的
    /// </summary>
    public object? value { get; init; }

    /// <summary>
    ///     类型名称的
    /// </summary>
    public string type_name => type_code switch
    {
        FbxConstants.PropertyType.boolean => "Boolean",
        FbxConstants.PropertyType.int8 => "Int8",
        FbxConstants.PropertyType.int16 => "Int16",
        FbxConstants.PropertyType.int32 => "Int32",
        FbxConstants.PropertyType.int64 => "Int64",
        FbxConstants.PropertyType.float32 => "Float32",
        FbxConstants.PropertyType.float64 => "Float64",
        FbxConstants.PropertyType.@string => "String",
        FbxConstants.PropertyType.raw_buffer => "RawBuffer",
        _ => $"Unknown(0x{type_code:X2})"
    };
}