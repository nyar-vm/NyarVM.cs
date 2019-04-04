namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 变换类型
///     。
/// </summary>
public enum SvgTransformType : byte
{
    /// <summary>
    ///     矩阵变换
    ///     。
    /// </summary>
    matrix = 0,


    /// <summary>
    ///     平移
    ///     。
    /// </summary>
    translate = 1,


    /// <summary>
    ///     缩放
    ///     。
    /// </summary>
    scale = 2,


    /// <summary>
    ///     旋转
    ///     。
    /// </summary>
    rotate = 3,


    /// <summary>
    ///     X 轴倾斜
    ///     。
    /// </summary>
    skew_x = 4,


    /// <summary>
    ///     Y 轴倾斜
    ///     。
    /// </summary>
    skew_y = 5
}