namespace Std.Data.Binary.Usd.Data;

/// <summary>
///     USD 场景数据的
/// </summary>
public sealed class UsdStageData
{
    /// <summary>
    ///     文件类型的
    /// </summary>
    public UsdFileType file_type { get; init; }

    /// <summary>
    ///     版本号的
    /// </summary>
    public int version { get; init; }

    /// <summary>
    ///     根层路径的
    /// </summary>
    public string root_layer_path { get; init; } = string.Empty;

    /// <summary>
    ///     Prim 列表的
    /// </summary>
    public IReadOnlyList<UsdPrimData> prims { get; init; } = [];
}

/// <summary>
///     USD Prim 数据的
/// </summary>
public sealed class UsdPrimData
{
    /// <summary>
    ///     Prim 路径的
    /// </summary>
    public string path { get; init; } = string.Empty;

    /// <summary>
    ///     Prim 类型名称的
    /// </summary>
    public string type_name { get; init; } = string.Empty;

    /// <summary>
    ///     Prim 名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     的Prim 列表的
    /// </summary>
    public IReadOnlyList<UsdPrimData> children { get; init; } = [];
}