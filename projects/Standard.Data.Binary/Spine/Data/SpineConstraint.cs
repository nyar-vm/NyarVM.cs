namespace Std.Data.Binary.Spine.Data;

/// <summary>
///     Spine IK 约束数据的
/// </summary>
public sealed class SpineIkConstraint
{
    /// <summary>
    ///     约束名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     受约束的骨骼名称列表的
    /// </summary>
    public IReadOnlyList<string> bones { get; init; } = [];

    /// <summary>
    ///     目标骨骼名称的
    /// </summary>
    public string target { get; init; } = string.Empty;

    /// <summary>
    ///     弯曲方向的 为正向，-1 为反向）的
    /// </summary>
    public int bend_direction { get; init; } = 1;

    /// <summary>
    ///     是否压缩的
    /// </summary>
    public bool compress { get; init; }

    /// <summary>
    ///     是否拉伸的
    /// </summary>
    public bool stretch { get; init; }

    /// <summary>
    ///     是否均匀缩放的
    /// </summary>
    public bool uniform { get; init; }

    /// <summary>
    ///     混合系数的
    /// </summary>
    public float mix { get; init; } = 1.0f;

    /// <summary>
    ///     软度的
    /// </summary>
    public float softness { get; init; }
}

/// <summary>
///     Spine 变换约束数据的
/// </summary>
public sealed class SpineTransformConstraint
{
    /// <summary>
    ///     约束名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     受约束的骨骼名称列表的
    /// </summary>
    public IReadOnlyList<string> bones { get; init; } = [];

    /// <summary>
    ///     目标骨骼名称的
    /// </summary>
    public string target { get; init; } = string.Empty;

    /// <summary>
    ///     旋转混合系数的
    /// </summary>
    public float rotate_mix { get; init; } = 1.0f;

    /// <summary>
    ///     位移混合系数的
    /// </summary>
    public float translate_mix { get; init; } = 1.0f;

    /// <summary>
    ///     缩放混合系数的
    /// </summary>
    public float scale_mix { get; init; } = 1.0f;

    /// <summary>
    ///     剪切混合系数的
    /// </summary>
    public float shear_mix { get; init; } = 1.0f;
}

/// <summary>
///     Spine 路径约束数据的
/// </summary>
public sealed class SpinePathConstraint
{
    /// <summary>
    ///     约束名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     受约束的骨骼名称列表的
    /// </summary>
    public IReadOnlyList<string> bones { get; init; } = [];

    /// <summary>
    ///     目标插槽名称的
    /// </summary>
    public string target { get; init; } = string.Empty;

    /// <summary>
    ///     位置模式的
    /// </summary>
    public float position_mode { get; init; }

    /// <summary>
    ///     间距模式的
    /// </summary>
    public float spacing_mode { get; init; }

    /// <summary>
    ///     旋转模式的
    /// </summary>
    public float rotate_mode { get; init; }

    /// <summary>
    ///     旋转角度的
    /// </summary>
    public float rotation { get; init; }

    /// <summary>
    ///     位置的
    /// </summary>
    public float position { get; init; }

    /// <summary>
    ///     间距的
    /// </summary>
    public float spacing { get; init; }

    /// <summary>
    ///     旋转混合系数的
    /// </summary>
    public float rotate_mix { get; init; } = 1.0f;

    /// <summary>
    ///     位移混合系数的
    /// </summary>
    public float translate_mix { get; init; } = 1.0f;
}