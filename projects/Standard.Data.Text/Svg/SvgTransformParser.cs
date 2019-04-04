namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 变换解析器，解析 transform 属性
///     。
/// </summary>
public sealed class SvgTransformParser
{
    /// <summary>
    ///     解析 SVG transform 属性字符串
    ///     。
    /// </summary>
    public static List<SvgTransform> parse(string transform)
    {
        if (string.IsNullOrWhiteSpace(transform)) return [];

        var results = new List<SvgTransform>();
        var span = transform.AsSpan();
        var i = 0;

        while (i < span.Length)
        {
            while (i < span.Length && !char.IsLetter(span[i])) i++;

            if (i >= span.Length) break;

            var nameStart = i;
            while (i < span.Length && char.IsLetter(span[i])) i++;

            var name = span[nameStart..i].ToString();

            while (i < span.Length && span[i] != '(') i++;

            if (i >= span.Length) break;

            i++;

            var argsStart = i;
            var depth = 1;

            while (i < span.Length && depth > 0)
            {
                if (span[i] == '(')
                    depth++;
                else if (span[i] == ')') depth--;

                i++;
            }

            var argsStr = span[argsStart..(i - 1)].ToString();
            var args = parse_arguments(argsStr);

            var transformType = name.ToLowerInvariant() switch
            {
                "matrix" => SvgTransformType.matrix,
                "translate" => SvgTransformType.translate,
                "scale" => SvgTransformType.scale,
                "rotate" => SvgTransformType.rotate,
                "skewx" => SvgTransformType.skew_x,
                "skewy" => SvgTransformType.skew_y,
                _ => (SvgTransformType?)null
            };

            if (transformType.HasValue) results.Add(new SvgTransform { type = transformType.Value, arguments = args });
        }

        return results;
    }

    private static float[] parse_arguments(string argsStr)
    {
        var args = new List<float>();
        var span = argsStr.AsSpan();
        var i = 0;

        while (i < span.Length)
        {
            while (i < span.Length && (char.IsWhiteSpace(span[i]) || span[i] is ',' or ';')) i++;

            if (i >= span.Length) break;

            var start = i;

            if (span[i] is '-' or '+') i++;

            while (i < span.Length && (char.IsDigit(span[i]) || span[i] is '.' or 'e' or 'E' or '-' or '+')) i++;

            var numStr = span[start..i];

            if (float.TryParse(numStr, out var value)) args.Add(value);
        }

        return [.. args];
    }
}