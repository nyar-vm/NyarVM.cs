namespace Std.Data.Binary.Live2D.Data;

/// <summary>
///     Live2D Cubism 模型数据，包含从 moc3 二进制文件解码的完整模型信息的
/// </summary>
/// <remarks>
///     moc3 格式采用 Structure of Arrays 范式，每个数据字段存储为独立的连续数组的
///     此数据模型将 SoA 格式重组为面向对象的 AOS 结构，便于上层使用的
/// </remarks>
public sealed class Live2DModelData
{
    /// <summary>
    ///     模型版本的, 4, 5）的
    /// </summary>
    public int version { get; init; }

    /// <summary>
    ///     是否为大端序的
    /// </summary>
    public bool is_big_endian { get; init; }

    /// <summary>
    ///     版本修订号的
    /// </summary>
    public int revision { get; init; }

    /// <summary>
    ///     模型文件路径的
    /// </summary>
    public string file_path { get; init; } = string.Empty;

    /// <summary>
    ///     画布信息的
    /// </summary>
    public Live2DCanvasInfo canvas { get; init; } = new();

    /// <summary>
    ///     参数列表的
    /// </summary>
    public IReadOnlyList<Live2DParameter> parameters { get; init; } = [];

    /// <summary>
    ///     部件列表的
    /// </summary>
    public IReadOnlyList<Live2DPart> parts { get; init; } = [];

    /// <summary>
    ///     绘制对象（ArtMesh）列表的
    /// </summary>
    public IReadOnlyList<Live2DDrawable> drawables { get; init; } = [];

    /// <summary>
    ///     变形器列表的
    /// </summary>
    public IReadOnlyList<Live2DDeformer> deformers { get; init; } = [];

    /// <summary>
    ///     纹理数量的
    /// </summary>
    public int texture_count { get; init; }
}

/// <summary>
///     Live2D 画布信息，对的moc3 段偏移表的CanvasInfo 段的
/// </summary>
public sealed class Live2DCanvasInfo
{
    /// <summary>
    ///     画布宽度的
    /// </summary>
    public float width { get; init; }

    /// <summary>
    ///     画布高度的
    /// </summary>
    public float height { get; init; }

    /// <summary>
    ///     画布中心 X 坐标的
    /// </summary>
    public float center_x { get; init; }

    /// <summary>
    ///     画布中心 Y 坐标的
    /// </summary>
    public float center_y { get; init; }

    /// <summary>
    ///     像素密度的
    /// </summary>
    public float pixels_per_unit { get; init; } = 1.0f;
}

/// <summary>
///     Live2D 参数数据，由 moc3 段偏移表的ParameterIds/ParameterMinimumValues/ParameterMaximumValues/ParameterDefaultValues 段重组而来的
/// </summary>
public sealed class Live2DParameter
{
    /// <summary>
    ///     参数标识符的
    /// </summary>
    public string id { get; init; } = string.Empty;

    /// <summary>
    ///     最小值的
    /// </summary>
    public float min_value { get; init; }

    /// <summary>
    ///     最大值的
    /// </summary>
    public float max_value { get; init; }

    /// <summary>
    ///     默认值的
    /// </summary>
    public float default_value { get; init; }
}

/// <summary>
///     Live2D 部件数据，由 moc3 段偏移表的PartIds/PartParentPartIndices 段重组而来的
/// </summary>
public sealed class Live2DPart
{
    /// <summary>
    ///     部件标识符的
    /// </summary>
    public string id { get; init; } = string.Empty;

    /// <summary>
    ///     父部件索引（-1 表示无父级）的
    /// </summary>
    public int parent_index { get; init; } = -1;
}

/// <summary>
///     Live2D 绘制对象（ArtMesh）数据，的moc3 段偏移表的DrawableIds/DrawableConstantFlags/DrawableTextureIndices 等段重组而来的
/// </summary>
public sealed class Live2DDrawable
{
    /// <summary>
    ///     绘制对象标识符的
    /// </summary>
    public string id { get; init; } = string.Empty;

    /// <summary>
    ///     纹理索引的
    /// </summary>
    public int texture_index { get; init; } = -1;

    /// <summary>
    ///     绘制顺序的
    /// </summary>
    public int draw_order { get; init; }

    /// <summary>
    ///     渲染顺序的
    /// </summary>
    public int render_order { get; init; }

    /// <summary>
    ///     顶点位置列表（每两个 float 为一个顶点的 X、Y 坐标）的
    /// </summary>
    public IReadOnlyList<float> vertex_positions { get; init; } = [];

    /// <summary>
    ///     顶点 UV 列表（每两个 float 为一的UV 的U、V 坐标）的
    /// </summary>
    public IReadOnlyList<float> vertex_uvs { get; init; } = [];

    /// <summary>
    ///     三角形索引列表的
    /// </summary>
    public IReadOnlyList<int> indices { get; init; } = [];

    /// <summary>
    ///     顶点数量的
    /// </summary>
    public int vertex_count { get; init; }

    /// <summary>
    ///     是否翻转 UV 的Y 轴的
    /// </summary>
    public bool flip_uv_y { get; init; }

    /// <summary>
    ///     混合模式的=Normal, 1=Additive, 2=Multiply）的
    /// </summary>
    public int blend_mode { get; init; }

    /// <summary>
    ///     不透明度的
    /// </summary>
    public float opacity { get; init; } = 1.0f;

    /// <summary>
    ///     遮罩绘制对象索引列表的
    /// </summary>
    public IReadOnlyList<int> mask_drawable_indices { get; init; } = [];
}

/// <summary>
///     Live2D 变形器数据，的moc3 段偏移表的DeformerIds/DeformerTypes/DeformerParentIndices 等段重组而来（v4+）的
/// </summary>
public sealed class Live2DDeformer
{
    /// <summary>
    ///     变形器标识符的
    /// </summary>
    public string id { get; init; } = string.Empty;

    /// <summary>
    ///     变形器类型（0=Rotation, 1=Warp, 2=Combined）的
    /// </summary>
    public int type { get; init; }

    /// <summary>
    ///     父变形器索引的1 表示无父级）的
    /// </summary>
    public int parent_index { get; init; } = -1;

    /// <summary>
    ///     变形器包围盒 X的
    /// </summary>
    public float x { get; init; }

    /// <summary>
    ///     变形器包围盒 Y的
    /// </summary>
    public float y { get; init; }

    /// <summary>
    ///     变形器包围盒宽度的
    /// </summary>
    public float width { get; init; }

    /// <summary>
    ///     变形器包围盒高度的
    /// </summary>
    public float height { get; init; }
}