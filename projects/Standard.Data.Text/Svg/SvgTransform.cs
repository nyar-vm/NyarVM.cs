namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 变换操作
///     。
/// </summary>
public sealed class SvgTransform
{
    /// <summary>
    ///     变换类型
    ///     。
    /// </summary>
    public SvgTransformType type { get; init; }


    /// <summary>
    ///     变换参数
    ///     <para>Matrix: [a, b, c, d, e, f]</para>
    ///     <para>Translate: [tx, ty?]（ty 默认 0）</para>
    ///     <para>Scale: [sx, sy?]（sy 默认等于 sx）</para>
    ///     <para>Rotate: [angle, cx?, cy?]（cx/cy 默认 0）</para>
    ///     <para>SkewX: [angle]</para>
    ///     <para>SkewY: [angle]</para>
    ///     。
    /// </summary>
    public float[] arguments { get; init; } = [];


    /// <summary>
    ///     转换为 3x2 仿射变换矩阵 [a, b, c, d, e, f]
    ///     。
    /// </summary>
    public float[] to_matrix()
    {
        return type switch
        {
            SvgTransformType.matrix => arguments.Length >= 6
                ? [arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5]]
                : [1f, 0f, 0f, 1f, 0f, 0f],
            SvgTransformType.translate => arguments.Length >= 2
                ? [1f, 0f, 0f, 1f, arguments[0], arguments[1]]
                : arguments.Length >= 1
                    ? [1f, 0f, 0f, 1f, arguments[0], 0f]
                    : [1f, 0f, 0f, 1f, 0f, 0f],
            SvgTransformType.scale => arguments.Length >= 2
                ? [arguments[0], 0f, 0f, arguments[1], 0f, 0f]
                : arguments.Length >= 1
                    ? [arguments[0], 0f, 0f, arguments[0], 0f, 0f]
                    : [1f, 0f, 0f, 1f, 0f, 0f],
            SvgTransformType.rotate => compute_rotation_matrix(),
            SvgTransformType.skew_x => arguments.Length >= 1
                ? [1f, 0f, MathF.Tan(arguments[0] * MathF.PI / 180f), 1f, 0f, 0f]
                : [1f, 0f, 0f, 1f, 0f, 0f],
            SvgTransformType.skew_y => arguments.Length >= 1
                ? [1f, MathF.Tan(arguments[0] * MathF.PI / 180f), 0f, 1f, 0f, 0f]
                : [1f, 0f, 0f, 1f, 0f, 0f],
            _ => [1f, 0f, 0f, 1f, 0f, 0f]
        };
    }

    private float[] compute_rotation_matrix()
    {
        if (arguments.Length < 1) return [1f, 0f, 0f, 1f, 0f, 0f];

        var angle = arguments[0] * MathF.PI / 180f;
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);

        if (arguments.Length >= 3)
        {
            var cx = arguments[1];
            var cy = arguments[2];
            return [cos, sin, -sin, cos, cx * (1 - cos) - cy * sin, cy * (1 - cos) + cx * sin];
        }

        return [cos, sin, -sin, cos, 0f, 0f];
    }
}