namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 路径命令类型
/// </summary>
public enum SvgPathCommandType : byte
{
    /// <summary>
    ///     移动到（绝对）
    /// </summary>
    move_to = 0,

    /// <summary>
    ///     移动到（相对）
    /// </summary>
    relative_move_to = 1,

    /// <summary>
    ///     直线到（绝对）
    /// </summary>
    line_to = 2,

    /// <summary>
    ///     直线到（相对）
    /// </summary>
    relative_line_to = 3,

    /// <summary>
    ///     水平线到（绝对）
    /// </summary>
    horizontal_line_to = 4,

    /// <summary>
    ///     水平线到（相对）
    /// </summary>
    relative_horizontal_line_to = 5,

    /// <summary>
    ///     垂直线到（绝对）
    /// </summary>
    vertical_line_to = 6,

    /// <summary>
    ///     垂直线到（相对）
    /// </summary>
    relative_vertical_line_to = 7,

    /// <summary>
    ///     三次贝塞尔曲线（绝对）
    /// </summary>
    curve_to = 8,

    /// <summary>
    ///     三次贝塞尔曲线（相对）
    /// </summary>
    relative_curve_to = 9,

    /// <summary>
    ///     平滑三次贝塞尔曲线（绝对）
    /// </summary>
    smooth_curve_to = 10,

    /// <summary>
    ///     平滑三次贝塞尔曲线（相对）
    /// </summary>
    relative_smooth_curve_to = 11,

    /// <summary>
    ///     二次贝塞尔曲线（绝对）
    /// </summary>
    quadratic_curve_to = 12,

    /// <summary>
    ///     二次贝塞尔曲线（相对）
    /// </summary>
    relative_quadratic_curve_to = 13,

    /// <summary>
    ///     平滑二次贝塞尔曲线（绝对）
    /// </summary>
    smooth_quadratic_curve_to = 14,

    /// <summary>
    ///     平滑二次贝塞尔曲线（相对）
    /// </summary>
    relative_smooth_quadratic_curve_to = 15,

    /// <summary>
    ///     弧线（绝对）
    /// </summary>
    arc_to = 16,

    /// <summary>
    ///     弧线（相对）
    /// </summary>
    relative_arc_to = 17,

    /// <summary>
    ///     闭合路径
    /// </summary>
    close_path = 18
}