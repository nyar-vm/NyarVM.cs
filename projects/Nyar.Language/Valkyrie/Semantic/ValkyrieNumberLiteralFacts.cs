using System.Globalization;

namespace Nyar.Language.Valkyrie.Semantic;

/// <summary>
///     Valkyrie 数值字面量工具，负责处理分隔下划线、显式类型后缀和默认数值类别识别。
/// </summary>
public static class ValkyrieNumberLiteralFacts
{
    private static readonly string[] s_numeric_suffixes =
    [
        "usize",
        "isize",
        "u128",
        "i128",
        "f128",
        "u64",
        "i64",
        "f64",
        "u32",
        "i32",
        "f32",
        "u16",
        "i16",
        "u8",
        "i8"
    ];

    /// <summary>
    ///     尝试从原始数字字面量文本中提取显式类型后缀。
    /// </summary>
    public static bool try_get_explicit_type_name(string? rawText, out string typeName)
    {
        typeName = string.Empty;
        if (!try_split_number_literal(rawText, out _, out var suffix))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(suffix))
        {
            return false;
        }

        typeName = suffix;
        return true;
    }

    /// <summary>
    ///     判断数字字面量是否应视为浮点数。
    /// </summary>
    public static bool is_floating_literal(string? rawText)
    {
        if (!try_split_number_literal(rawText, out var digitsText, out var suffix))
        {
            return false;
        }

        if (suffix is "f32" or "f64" or "f128")
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(suffix))
        {
            return false;
        }

        return digitsText.Contains('.', StringComparison.Ordinal)
               || digitsText.Contains('e', StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     尝试将整数字面量解析为 <see cref="long" />。
    /// </summary>
    public static bool try_parse_i64(string? rawText, out long value)
    {
        value = 0;
        if (!try_split_number_literal(rawText, out var digitsText, out var suffix))
        {
            return false;
        }

        if (suffix is "f32" or "f64" or "f128")
        {
            return false;
        }

        var normalizedDigits = digitsText.Replace("_", string.Empty, StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(normalizedDigits))
        {
            return false;
        }

        var numberBase = 10;
        if (normalizedDigits.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            normalizedDigits = normalizedDigits[2..];
            numberBase = 16;
        }
        else if (normalizedDigits.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            normalizedDigits = normalizedDigits[2..];
            numberBase = 2;
        }
        else if (normalizedDigits.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
        {
            normalizedDigits = normalizedDigits[2..];
            numberBase = 8;
        }

        if (string.IsNullOrWhiteSpace(normalizedDigits))
        {
            return false;
        }

        return try_parse_integer_with_base(normalizedDigits, numberBase, out value);
    }

    /// <summary>
    ///     尝试将浮点字面量解析为 <see cref="double" />。
    /// </summary>
    public static bool try_parse_f64(string? rawText, out double value)
    {
        value = 0d;
        if (!try_split_number_literal(rawText, out var digitsText, out _))
        {
            return false;
        }

        var normalizedDigits = digitsText.Replace("_", string.Empty, StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(normalizedDigits))
        {
            return false;
        }

        return double.TryParse(normalizedDigits, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool try_split_number_literal(string? rawText, out string digitsText, out string? suffix)
    {
        digitsText = string.Empty;
        suffix = null;
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return false;
        }

        foreach (var candidate in s_numeric_suffixes)
        {
            if (rawText.EndsWith($"_{candidate}", StringComparison.OrdinalIgnoreCase))
            {
                digitsText = rawText[..^(candidate.Length + 1)];
                suffix = candidate;
                return !string.IsNullOrWhiteSpace(digitsText);
            }

            if (rawText.EndsWith(candidate, StringComparison.OrdinalIgnoreCase))
            {
                var digitsLength = rawText.Length - candidate.Length;
                if (digitsLength <= 0)
                {
                    continue;
                }

                var boundary = rawText[digitsLength - 1];
                if (char.IsLetter(boundary))
                {
                    continue;
                }

                digitsText = rawText[..digitsLength];
                suffix = candidate;
                return !string.IsNullOrWhiteSpace(digitsText);
            }
        }

        digitsText = rawText;
        return true;
    }

    private static bool try_parse_integer_with_base(string digitsText, int numberBase, out long value)
    {
        value = 0;
        if (numberBase == 10)
        {
            return long.TryParse(digitsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        try
        {
            checked
            {
                long result = 0;
                foreach (var digit in digitsText)
                {
                    var digitValue = digit switch
                    {
                        >= '0' and <= '9' => digit - '0',
                        >= 'a' and <= 'f' => digit - 'a' + 10,
                        >= 'A' and <= 'F' => digit - 'A' + 10,
                        _ => -1
                    };

                    if (digitValue < 0 || digitValue >= numberBase)
                    {
                        return false;
                    }

                    result = result * numberBase + digitValue;
                }

                value = result;
                return true;
            }
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
