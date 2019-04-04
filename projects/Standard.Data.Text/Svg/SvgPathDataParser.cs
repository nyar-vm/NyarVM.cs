namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 路径数据解析器，解析 SVG path 元素的 d 属性
///     。
/// </summary>
public sealed class SvgPathDataParser
{
    /// <summary>
    ///     解析 SVG 路径数据字符串
    ///     。
    /// </summary>
    public static List<SvgPathCommand> parse(string d)
    {
        if (string.IsNullOrWhiteSpace(d)) return [];

        var commands = new List<SvgPathCommand>();
        var span = d.AsSpan();
        var i = 0;

        while (i < span.Length)
        {
            i = skip_whitespace(span, i);

            if (i >= span.Length) break;

            var c = span[i];

            if (!is_command_char(c))
            {
                i++;
                continue;
            }

            var commandType = parse_command_type(c);
            i++;

            if (commandType == SvgPathCommandType.close_path)
            {
                commands.Add(new SvgPathCommand { type = commandType });
                continue;
            }

            var paramCount = get_parameter_count(commandType);

            while (i < span.Length)
            {
                var start = i;
                i = skip_whitespace(span, i);

                if (i >= span.Length) break;

                if (is_command_char(span[i])) break;

                var args = new float[paramCount];
                var success = true;

                for (var p = 0; p < paramCount; p++)
                {
                    i = skip_whitespace(span, i);

                    if (i >= span.Length)
                    {
                        success = false;
                        break;
                    }

                    if (!try_parse_number(span, ref i, out var number))
                    {
                        success = false;
                        break;
                    }

                    args[p] = number;
                }

                if (!success) break;

                commands.Add(new SvgPathCommand { type = commandType, arguments = args });

                if (commandType is SvgPathCommandType.move_to)
                    commandType = SvgPathCommandType.line_to;
                else if (commandType is SvgPathCommandType.relative_move_to)
                    commandType = SvgPathCommandType.relative_line_to;
            }
        }

        return commands;
    }

    private static SvgPathCommandType parse_command_type(char c)
    {
        return c switch
        {
            'M' => SvgPathCommandType.move_to,
            'm' => SvgPathCommandType.relative_move_to,
            'L' => SvgPathCommandType.line_to,
            'l' => SvgPathCommandType.relative_line_to,
            'H' => SvgPathCommandType.horizontal_line_to,
            'h' => SvgPathCommandType.relative_horizontal_line_to,
            'V' => SvgPathCommandType.vertical_line_to,
            'v' => SvgPathCommandType.relative_vertical_line_to,
            'C' => SvgPathCommandType.curve_to,
            'c' => SvgPathCommandType.relative_curve_to,
            'S' => SvgPathCommandType.smooth_curve_to,
            's' => SvgPathCommandType.relative_smooth_curve_to,
            'Q' => SvgPathCommandType.quadratic_curve_to,
            'q' => SvgPathCommandType.relative_quadratic_curve_to,
            'T' => SvgPathCommandType.smooth_quadratic_curve_to,
            't' => SvgPathCommandType.relative_smooth_quadratic_curve_to,
            'A' => SvgPathCommandType.arc_to,
            'a' => SvgPathCommandType.relative_arc_to,
            'Z' or 'z' => SvgPathCommandType.close_path,
            _ => SvgPathCommandType.close_path
        };
    }

    private static int get_parameter_count(SvgPathCommandType type)
    {
        return type switch
        {
            SvgPathCommandType.move_to or SvgPathCommandType.relative_move_to => 2,
            SvgPathCommandType.line_to or SvgPathCommandType.relative_line_to => 2,
            SvgPathCommandType.horizontal_line_to or SvgPathCommandType.relative_horizontal_line_to => 1,
            SvgPathCommandType.vertical_line_to or SvgPathCommandType.relative_vertical_line_to => 1,
            SvgPathCommandType.curve_to or SvgPathCommandType.relative_curve_to => 6,
            SvgPathCommandType.smooth_curve_to or SvgPathCommandType.relative_smooth_curve_to => 4,
            SvgPathCommandType.quadratic_curve_to or SvgPathCommandType.relative_quadratic_curve_to => 4,
            SvgPathCommandType.smooth_quadratic_curve_to or SvgPathCommandType.relative_smooth_quadratic_curve_to => 2,
            SvgPathCommandType.arc_to or SvgPathCommandType.relative_arc_to => 7,
            _ => 0
        };
    }

    private static bool is_command_char(char c)
    {
        return c is 'M' or 'm' or 'L' or 'l' or 'H' or 'h' or 'V' or 'v'
            or 'C' or 'c' or 'S' or 's' or 'Q' or 'q' or 'T' or 't'
            or 'A' or 'a' or 'Z' or 'z';
    }

    private static int skip_whitespace(ReadOnlySpan<char> span, int index)
    {
        while (index < span.Length && char.IsWhiteSpace(span[index])) index++;

        return index;
    }

    private static bool try_parse_number(ReadOnlySpan<char> span, ref int index, out float value)
    {
        value = 0;

        if (index >= span.Length) return false;

        var start = index;
        var sign = 1f;

        if (span[index] is '-' or '+')
        {
            if (span[index] == '-') sign = -1;

            index++;
        }

        var hasDigits = false;
        var intPart = 0f;
        var fracPart = 0f;
        var fracDivisor = 1f;
        var hasExponent = false;
        var expSign = 1;
        var exponent = 0;

        while (index < span.Length && char.IsDigit(span[index]))
        {
            intPart = intPart * 10 + (span[index] - '0');
            hasDigits = true;
            index++;
        }

        if (index < span.Length && span[index] == '.')
        {
            index++;

            while (index < span.Length && char.IsDigit(span[index]))
            {
                fracPart = fracPart * 10 + (span[index] - '0');
                fracDivisor *= 10;
                hasDigits = true;
                index++;
            }
        }

        if (!hasDigits)
        {
            index = start;
            return false;
        }

        if (index < span.Length && span[index] is 'e' or 'E')
        {
            hasExponent = true;
            index++;

            if (index < span.Length && span[index] is '-' or '+')
            {
                if (span[index] == '-') expSign = -1;

                index++;
            }

            while (index < span.Length && char.IsDigit(span[index]))
            {
                exponent = exponent * 10 + (span[index] - '0');
                index++;
            }
        }

        value = sign * (intPart + fracPart / fracDivisor);

        if (hasExponent) value *= MathF.Pow(10, expSign * exponent);

        if (index < span.Length && span[index] is ',') index++;

        return true;
    }
}