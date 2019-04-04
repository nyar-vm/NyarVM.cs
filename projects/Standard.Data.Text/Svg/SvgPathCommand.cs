namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 路径命令
///     。
/// </summary>
public sealed class SvgPathCommand
{
    /// <summary>
    ///     命令类型
    ///     。
    /// </summary>
    public SvgPathCommandType type { get; init; }


    /// <summary>
    ///     命令参数
    ///     <para>M/m: [x, y]</para>
    ///     <para>L/l: [x, y]</para>
    ///     <para>H/h: [x]</para>
    ///     <para>V/v: [y]</para>
    ///     <para>C/c: [x1, y1, x2, y2, x, y]</para>
    ///     <para>S/s: [x2, y2, x, y]</para>
    ///     <para>Q/q: [x1, y1, x, y]</para>
    ///     <para>T/t: [x, y]</para>
    ///     <para>A/a: [rx, ry, xRotation, largeArc, sweep, x, y]</para>
    ///     <para>Z/z: 无参数</para>
    ///     。
    /// </summary>
    public float[] arguments { get; init; } = [];


    /// <summary>
    ///     是否为绝对坐标命令
    ///     。
    /// </summary>
    public bool is_absolute => type is >= SvgPathCommandType.move_to and <= SvgPathCommandType.arc_to
                               && (int)type % 2 == 0;
}