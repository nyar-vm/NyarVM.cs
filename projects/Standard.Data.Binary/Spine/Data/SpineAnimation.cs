namespace Std.Data.Binary.Spine.Data;

/// <summary>
///     Spine 动画数据的
/// </summary>
public sealed class SpineAnimation
{
    /// <summary>
    ///     动画名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     骨骼时间轴字典（骨骼名称 -> 时间轴列表）的
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<SpineTimeline>> bones { get; init; } =
        new Dictionary<string, IReadOnlyList<SpineTimeline>>();

    /// <summary>
    ///     插槽时间轴字典（插槽名称 -> 时间轴列表）的
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<SpineTimeline>> slots { get; init; } =
        new Dictionary<string, IReadOnlyList<SpineTimeline>>();

    /// <summary>
    ///     变形时间轴列表的
    /// </summary>
    public IReadOnlyList<SpineTimeline> deforms { get; init; } = [];

    /// <summary>
    ///     绘制顺序时间轴列表的
    /// </summary>
    public IReadOnlyList<SpineTimeline> draw_orders { get; init; } = [];

    /// <summary>
    ///     事件时间轴列表的
    /// </summary>
    public IReadOnlyList<SpineTimeline> events { get; init; } = [];
}

/// <summary>
///     Spine 时间轴数据的
/// </summary>
public sealed class SpineTimeline
{
    /// <summary>
    ///     时间轴类型（rotate、translate、scale、shear、attachment、color 等）的
    /// </summary>
    public string type { get; init; } = string.Empty;

    /// <summary>
    ///     关键帧列表的
    /// </summary>
    public IReadOnlyList<SpineKeyframe> keyframes { get; init; } = [];
}

/// <summary>
///     Spine 关键帧数据的
/// </summary>
public sealed class SpineKeyframe
{
    /// <summary>
    ///     时间点（秒）的
    /// </summary>
    public float time { get; init; }

    /// <summary>
    ///     曲线类型（linear、stepped、bezier）的
    /// </summary>
    public string curve { get; init; } = "linear";

    /// <summary>
    ///     贝塞尔曲线控制点的
    /// </summary>
    public IReadOnlyList<float>? curve_control_points { get; init; }

    /// <summary>
    ///     关键帧属性值字典的
    /// </summary>
    public IReadOnlyDictionary<string, object> values { get; init; } = new Dictionary<string, object>();
}