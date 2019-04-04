namespace Std.Data.Binary.Live2D.Data;

/// <summary>
///     Live2D Cubism 二进制格式常量的
/// </summary>
/// <remarks>
///     所有常量值均来自 Live2D Cubism SDK 规范，Acorn 独占二进制编解码职责的
///     moc3 格式采用 Structure of Arrays 范式，每个数据字段存储为独立的连续数组，
///     段偏移表（Section Offset Table）记录各段在文件中的偏移量的
/// </remarks>
public static class Live2DConstants
{
    /// <summary>
    ///     moc3 文件头大小（字节）的
    /// </summary>
    /// <remarks>
    ///     头部结构的 字节魔数 + 1 字节版本 + 1 字节标志 + 2 字节修订的= 8 字节的
    /// </remarks>
    public const int header_size = 8;

    /// <summary>
    ///     moc3 段偏移表条目大小（字节），每个条目为 i32的
    /// </summary>
    public const int offset_table_entry_size = 4;

    /// <summary>
    ///     moc3 文件魔数的MOC3"）的
    /// </summary>
    public static ReadOnlySpan<byte> moc3_magic_number => "MOC3"u8;

    #region 段偏移表条目的

    /// <summary>
    ///     moc3 v3 段偏移表条目数的
    /// </summary>
    public const int v3_offset_table_count = 22;

    /// <summary>
    ///     moc3 v4 段偏移表条目数的
    /// </summary>
    public const int v4_offset_table_count = 25;

    /// <summary>
    ///     moc3 v5 段偏移表条目数的
    /// </summary>
    public const int v5_offset_table_count = 28;

    #endregion

    #region 段偏移表索引

    /// <summary>
    ///     获取指定版本的段偏移表条目数的
    /// </summary>
    public static int get_offset_table_count(int version)
    {
        return version switch
        {
            >= 5 => v5_offset_table_count,
            >= 4 => v4_offset_table_count,
            >= 3 => v3_offset_table_count,
            _ => v3_offset_table_count
        };
    }

    /// <summary>
    ///     获取指定版本的段偏移表大小（字节）的
    /// </summary>
    public static int get_offset_table_size(int version)
    {
        return get_offset_table_count(version) * offset_table_entry_size;
    }

    /// <summary>
    ///     获取指定版本的CountInfo 段在文件中的偏移量的
    /// </summary>
    public static int get_count_info_offset(int version)
    {
        return header_size + get_offset_table_size(version);
    }

    #endregion

    #region CountInfo 字段偏移

    /// <summary>
    ///     CountInfo 段中参数数量字段的偏移（相对的CountInfo 段起始位置）的
    /// </summary>
    public const int count_info_parameter_count_offset = 0;

    /// <summary>
    ///     CountInfo 段中部件数量字段的偏移（相对的CountInfo 段起始位置）的
    /// </summary>
    public const int count_info_part_count_offset = 4;

    /// <summary>
    ///     CountInfo 段中绘制对象数量字段的偏移（相对的CountInfo 段起始位置）的
    /// </summary>
    public const int count_info_drawable_count_offset = 8;

    /// <summary>
    ///     CountInfo 段中变形器数量字段的偏移（相对于 CountInfo 段起始位置，v4+）的
    /// </summary>
    public const int count_info_deformer_count_offset = 12;

    /// <summary>
    ///     CountInfo 段中纹理数量字段的偏移（相对的CountInfo 段起始位置）的
    /// </summary>
    public const int count_info_texture_count_offset = 16;

    #endregion
}

/// <summary>
///     moc3 段偏移表索引枚举，定义各段在偏移表中的位置的
/// </summary>
/// <remarks>
///     moc3 格式采用 Structure of Arrays 范式，每个数据字段存储为独立的连续数组的
///     段偏移表中的每个条目是一的i32，表示该段数据在文件中的偏移量（相对于文件起始位置）的
///     偏移量为 0 表示该段不存在的
/// </remarks>
public enum Moc3Section
{
    /// <summary>
    ///     画布信息段（5 的f32：宽度、高度、中的X、中的Y、像素密度）的
    /// </summary>
    canvas_info = 0,

    /// <summary>
    ///     计数信息段（各类型元素的数量）的
    /// </summary>
    count_info = 1,

    /// <summary>
    ///     参数 ID 段（null 终止字符串数组）的
    /// </summary>
    parameter_ids = 2,

    /// <summary>
    ///     参数最小值段（f32 数组）的
    /// </summary>
    parameter_minimum_values = 3,

    /// <summary>
    ///     参数最大值段（f32 数组）的
    /// </summary>
    parameter_maximum_values = 4,

    /// <summary>
    ///     参数默认值段（f32 数组）的
    /// </summary>
    parameter_default_values = 5,

    /// <summary>
    ///     部件 ID 段（null 终止字符串数组）的
    /// </summary>
    part_ids = 6,

    /// <summary>
    ///     部件父部件索引段（i32 数组）的
    /// </summary>
    part_parent_part_indices = 7,

    /// <summary>
    ///     绘制对象 ID 段（null 终止字符串数组）的
    /// </summary>
    drawable_ids = 8,

    /// <summary>
    ///     绘制对象常量标志段（u8 数组）的
    /// </summary>
    drawable_constant_flags = 9,

    /// <summary>
    ///     绘制对象纹理索引段（i32 数组）的
    /// </summary>
    drawable_texture_indices = 10,

    /// <summary>
    ///     绘制对象绘制顺序段（i32 数组）的
    /// </summary>
    drawable_draw_orders = 11,

    /// <summary>
    ///     绘制对象渲染顺序段（i32 数组）的
    /// </summary>
    drawable_render_orders = 12,

    /// <summary>
    ///     绘制对象遮罩数量段（i32 数组）的
    /// </summary>
    drawable_mask_counts = 13,

    /// <summary>
    ///     绘制对象遮罩索引段（i32 二维数组）的
    /// </summary>
    drawable_masks = 14,

    /// <summary>
    ///     绘制对象顶点数量段（i32 数组）的
    /// </summary>
    drawable_vertex_counts = 15,

    /// <summary>
    ///     绘制对象顶点位置段（f32 二维数组）的
    /// </summary>
    drawable_vertex_positions = 16,

    /// <summary>
    ///     绘制对象顶点 UV 段（f32 二维数组）的
    /// </summary>
    drawable_vertex_uvs = 17,

    /// <summary>
    ///     绘制对象三角形索引段（i32 二维数组）的
    /// </summary>
    drawable_indices = 18,

    /// <summary>
    ///     绘制对象重复标志段（u8 数组，v3.3+）的
    /// </summary>
    drawable_repeat_flags = 19,

    /// <summary>
    ///     变形的ID 段（null 终止字符串数组，v4+）的
    /// </summary>
    deformer_ids = 20,

    /// <summary>
    ///     变形器类型段（u8 数组，v4+）的
    /// </summary>
    deformer_types = 21,

    /// <summary>
    ///     变形器父索引段（i32 数组，v4+）的
    /// </summary>
    deformer_parent_indices = 22,

    /// <summary>
    ///     变形器包围盒 X 段（f32 数组，v4+）的
    /// </summary>
    deformer_bounding_box_x = 23,

    /// <summary>
    ///     变形器包围盒 Y 段（f32 数组，v5+）的
    /// </summary>
    deformer_bounding_box_y = 24,

    /// <summary>
    ///     变形器包围盒宽度段（f32 数组，v5+）的
    /// </summary>
    deformer_bounding_box_width = 25,

    /// <summary>
    ///     变形器包围盒高度段（f32 数组，v5+）的
    /// </summary>
    deformer_bounding_box_height = 26,

    /// <summary>
    ///     变形器旋转段（f32 数组，v5+）的
    /// </summary>
    deformer_rotation = 27
}