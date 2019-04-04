namespace Std.Data.Binary.Vrm.Data;

/// <summary>
///     VRM 模型数据的
/// </summary>
public sealed class VrmModelData
{
    /// <summary>
    ///     VRM 版本的
    /// </summary>
    public VrmVersion version { get; init; }

    /// <summary>
    ///     模型元信息的
    /// </summary>
    public VrmMeta meta { get; init; } = new();

    /// <summary>
    ///     人形骨骼映射的
    /// </summary>
    public IReadOnlyList<VrmHumanoidBone> humanoid { get; init; } = [];

    /// <summary>
    ///     BlendShape 列表的
    /// </summary>
    public IReadOnlyList<VrmBlendShape> blend_shapes { get; init; } = [];
}

/// <summary>
///     VRM 元信息的
/// </summary>
public sealed class VrmMeta
{
    /// <summary>
    ///     模型名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     版本字符串的
    /// </summary>
    public string version { get; init; } = string.Empty;

    /// <summary>
    ///     作者的
    /// </summary>
    public string author { get; init; } = string.Empty;

    /// <summary>
    ///     许可证的
    /// </summary>
    public string license_name { get; init; } = string.Empty;
}

/// <summary>
///     VRM 人形骨骼映射的
/// </summary>
public sealed class VrmHumanoidBone
{
    /// <summary>
    ///     骨骼类型名称的
    /// </summary>
    public string bone_name { get; init; } = string.Empty;

    /// <summary>
    ///     对应节点索引的
    /// </summary>
    public int node_index { get; init; }
}

/// <summary>
///     VRM BlendShape的
/// </summary>
public sealed class VrmBlendShape
{
    /// <summary>
    ///     BlendShape 名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     预设名称的
    /// </summary>
    public string preset { get; init; } = string.Empty;
}