namespace Std.Data.Binary.Spine.Data;

/// <summary>
///     Spine 项目数据，包含骨骼、插槽、皮肤、动画等完整信息的
/// </summary>
public sealed class SpineProjectData
{
    /// <summary>
    ///     骨骼格式版本的
    /// </summary>
    public string skeleton_version { get; init; } = string.Empty;

    /// <summary>
    ///     项目哈希值的
    /// </summary>
    public string hash { get; init; } = string.Empty;

    /// <summary>
    ///     画布宽度的
    /// </summary>
    public float width { get; init; }

    /// <summary>
    ///     画布高度的
    /// </summary>
    public float height { get; init; }

    /// <summary>
    ///     Spine 编辑器版本的
    /// </summary>
    public string spine_version { get; init; } = string.Empty;

    /// <summary>
    ///     骨骼列表的
    /// </summary>
    public IReadOnlyList<SpineBone> bones { get; init; } = [];

    /// <summary>
    ///     插槽列表的
    /// </summary>
    public IReadOnlyList<SpineSlot> slots { get; init; } = [];

    /// <summary>
    ///     皮肤列表的
    /// </summary>
    public IReadOnlyList<SpineSkin> skins { get; init; } = [];

    /// <summary>
    ///     动画列表的
    /// </summary>
    public IReadOnlyList<SpineAnimation> animations { get; init; } = [];

    /// <summary>
    ///     事件列表的
    /// </summary>
    public IReadOnlyList<SpineEvent> events { get; init; } = [];

    /// <summary>
    ///     IK 约束列表的
    /// </summary>
    public IReadOnlyList<SpineIkConstraint> ik_constraints { get; init; } = [];

    /// <summary>
    ///     变换约束列表的
    /// </summary>
    public IReadOnlyList<SpineTransformConstraint> transform_constraints { get; init; } = [];

    /// <summary>
    ///     路径约束列表的
    /// </summary>
    public IReadOnlyList<SpinePathConstraint> path_constraints { get; init; } = [];
}