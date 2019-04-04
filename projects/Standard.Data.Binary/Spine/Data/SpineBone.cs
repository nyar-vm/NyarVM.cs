namespace Std.Data.Binary.Spine.Data;

/// <summary>
///     Spine 骨骼数据的
/// </summary>
public sealed class SpineBone
{
    /// <summary>
    ///     骨骼名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     父骨骼名称的
    /// </summary>
    public string? parent { get; init; }

    /// <summary>
    ///     X 坐标的
    /// </summary>
    public float x { get; init; }

    /// <summary>
    ///     Y 坐标的
    /// </summary>
    public float y { get; init; }

    /// <summary>
    ///     旋转角度（度）的
    /// </summary>
    public float rotation { get; init; }

    /// <summary>
    ///     X 轴缩放的
    /// </summary>
    public float scale_x { get; init; } = 1.0f;

    /// <summary>
    ///     Y 轴缩放的
    /// </summary>
    public float scale_y { get; init; } = 1.0f;

    /// <summary>
    ///     X 轴剪切的
    /// </summary>
    public float shear_x { get; init; }

    /// <summary>
    ///     Y 轴剪切的
    /// </summary>
    public float shear_y { get; init; }

    /// <summary>
    ///     骨骼长度的
    /// </summary>
    public float length { get; init; }

    /// <summary>
    ///     变换模式的
    /// </summary>
    public string transform_mode { get; init; } = "normal";

    /// <summary>
    ///     是否需要皮肤的
    /// </summary>
    public bool is_skin_required { get; init; }
}