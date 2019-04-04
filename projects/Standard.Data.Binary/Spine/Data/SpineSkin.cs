namespace Std.Data.Binary.Spine.Data;

/// <summary>
///     Spine 皮肤数据的
/// </summary>
public sealed class SpineSkin
{
    /// <summary>
    ///     皮肤名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     附件列表的
    /// </summary>
    public IReadOnlyList<SpineSkinAttachment> attachments { get; init; } = [];
}

/// <summary>
///     Spine 皮肤附件数据的
/// </summary>
public sealed class SpineSkinAttachment
{
    /// <summary>
    ///     插槽名称的
    /// </summary>
    public string slot_name { get; init; } = string.Empty;

    /// <summary>
    ///     附件名称的
    /// </summary>
    public string attachment_name { get; init; } = string.Empty;

    /// <summary>
    ///     附件类型（region、mesh、weightedmesh 等）的
    /// </summary>
    public string type { get; init; } = "region";

    /// <summary>
    ///     资源路径的
    /// </summary>
    public string? path { get; init; }

    /// <summary>
    ///     X 坐标的
    /// </summary>
    public float x { get; init; }

    /// <summary>
    ///     Y 坐标的
    /// </summary>
    public float y { get; init; }

    /// <summary>
    ///     旋转角度的
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
    ///     宽度的
    /// </summary>
    public float width { get; init; }

    /// <summary>
    ///     高度的
    /// </summary>
    public float height { get; init; }

    /// <summary>
    ///     顶点数据（网格类型使用）的
    /// </summary>
    public IReadOnlyList<float>? vertices { get; init; }

    /// <summary>
    ///     三角形索引（网格类型使用）的
    /// </summary>
    public IReadOnlyList<int>? triangles { get; init; }

    /// <summary>
    ///     UV 坐标（网格类型使用）的
    /// </summary>
    public IReadOnlyList<float>? uvs { get; init; }
}