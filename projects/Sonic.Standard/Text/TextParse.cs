using Std.Category;
using Std.Text.Utf8;

namespace Std.Text;

/// <summary>
///     文本解析工具，提供基本类型从 Utf8Text 解析的能力
/// </summary>
public static class TextParse
{
    #region 布尔解析

    /// <summary>
    ///     从 Utf8Text 解析 bool（不区分大小写，"true" 为真，"false" 为假）
    /// </summary>
    /// <param name="text">要解析的文本</param>
    /// <returns>解析成功返回 Some，否则返回 None</returns>
    public static Option<bool> parse_bool(Utf8Text text)
    {
        var lower = text.to_lower();

        if (lower.byte_length == 4 &&
            lower.as_span()[0] == (byte)'t' &&
            lower.as_span()[1] == (byte)'r' &&
            lower.as_span()[2] == (byte)'u' &&
            lower.as_span()[3] == (byte)'e')
            return Option<bool>.some(true);

        if (lower.byte_length == 5 &&
            lower.as_span()[0] == (byte)'f' &&
            lower.as_span()[1] == (byte)'a' &&
            lower.as_span()[2] == (byte)'l' &&
            lower.as_span()[3] == (byte)'s' &&
            lower.as_span()[4] == (byte)'e')
            return Option<bool>.some(false);

        return Option<bool>.none;
    }

    #endregion

    #region 整数解析

    /// <summary>
    ///     从 Utf8Text 解析 int
    /// </summary>
    /// <param name="text">要解析的文本</param>
    /// <returns>解析成功返回 Some，否则返回 None</returns>
    public static Option<int> parse_i32(Utf8Text text)
    {
        var span = text.as_span();

        if (span.IsEmpty) return Option<int>.none;

        var index = 0;
        var negative = false;

        if (span[0] == (byte)'-')
        {
            negative = true;
            index++;
        }
        else if (span[0] == (byte)'+')
        {
            index++;
        }

        if (index >= span.Length || !is_digit(span[index])) return Option<int>.none;

        var value = 0L;

        while (index < span.Length && is_digit(span[index]))
        {
            var digit = span[index] - (byte)'0';
            value = value * 10 + digit;

            if (!negative && value > int.MaxValue) return Option<int>.none;

            if (negative && -value < int.MinValue) return Option<int>.none;

            index++;
        }

        if (index < span.Length) return Option<int>.none;

        return Option<int>.some(negative ? (int)-value : (int)value);
    }

    /// <summary>
    ///     从 Utf8Text 解析 long
    /// </summary>
    /// <param name="text">要解析的文本</param>
    /// <returns>解析成功返回 Some，否则返回 None</returns>
    public static Option<long> parse_i64(Utf8Text text)
    {
        var span = text.as_span();

        if (span.IsEmpty) return Option<long>.none;

        var index = 0;
        var negative = false;

        if (span[0] == (byte)'-')
        {
            negative = true;
            index++;
        }
        else if (span[0] == (byte)'+')
        {
            index++;
        }

        if (index >= span.Length || !is_digit(span[index])) return Option<long>.none;

        long value = 0;
        var prevValue = 0L;

        while (index < span.Length && is_digit(span[index]))
        {
            var digit = span[index] - (byte)'0';
            prevValue = value;
            value = value * 10 + digit;

            if (value < prevValue) return Option<long>.none;

            index++;
        }

        if (index < span.Length) return Option<long>.none;

        if (negative)
        {
            if (value == long.MinValue) return Option<long>.some(long.MinValue);

            return Option<long>.some(-value);
        }

        if (value < 0) return Option<long>.none;

        return Option<long>.some(value);
    }

    #endregion

    #region 浮点解析

    /// <summary>
    ///     从 Utf8Text 解析 double
    /// </summary>
    /// <param name="text">要解析的文本</param>
    /// <returns>解析成功返回 Some，否则返回 None</returns>
    public static Option<double> parse_f64(Utf8Text text)
    {
        var span = text.as_span();

        if (span.IsEmpty) return Option<double>.none;

        var index = 0;
        var negative = false;

        if (span[0] == (byte)'-')
        {
            negative = true;
            index++;
        }
        else if (span[0] == (byte)'+')
        {
            index++;
        }

        if (index >= span.Length) return Option<double>.none;

        var intPart = 0L;
        var hasIntPart = false;

        while (index < span.Length && is_digit(span[index]))
        {
            var digit = span[index] - (byte)'0';
            intPart = intPart * 10 + digit;
            hasIntPart = true;
            index++;
        }

        double fracPart = 0;
        double fracDiv = 1;
        var hasFracPart = false;

        if (index < span.Length && span[index] == (byte)'.')
        {
            index++;

            while (index < span.Length && is_digit(span[index]))
            {
                fracDiv *= 10;
                fracPart = fracPart * 10 + (span[index] - (byte)'0');
                hasFracPart = true;
                index++;
            }
        }

        if (!hasIntPart && !hasFracPart) return Option<double>.none;

        var value = intPart + fracPart / fracDiv;

        if (index < span.Length && (span[index] == (byte)'e' || span[index] == (byte)'E'))
        {
            index++;
            var expNegative = false;

            if (index < span.Length && span[index] == (byte)'-')
            {
                expNegative = true;
                index++;
            }
            else if (index < span.Length && span[index] == (byte)'+')
            {
                index++;
            }

            var exponent = 0;

            while (index < span.Length && is_digit(span[index]))
            {
                exponent = exponent * 10 + (span[index] - (byte)'0');
                index++;
            }

            var pow = exp_pow10(exponent);

            if (expNegative)
                value /= pow;
            else
                value *= pow;
        }

        if (index < span.Length) return Option<double>.none;

        if (negative) value = -value;

        return Option<double>.some(value);
    }

    /// <summary>
    ///     从 Utf8Text 解析 float
    /// </summary>
    /// <param name="text">要解析的文本</param>
    /// <returns>解析成功返回 Some，否则返回 None</returns>
    public static Option<float> parse_f32(Utf8Text text)
    {
        var result = parse_f64(text);

        if (result.is_none) return Option<float>.none;

        return Option<float>.some((float)result.value);
    }

    #endregion

    #region 辅助方法

    private static bool is_digit(byte b)
    {
        return b is >= (byte)'0' and <= (byte)'9';
    }

    private static double exp_pow10(int exponent)
    {
        var result = 1.0;

        for (var i = 0; i < exponent; i++) result *= 10.0;

        return result;
    }

    #endregion
}