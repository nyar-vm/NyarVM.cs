using System.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     字符串转义工具类，用于源代码生成器内部。
/// </summary>
internal static class StringEscapeHelper
{
    /// <summary>
    ///     将字符串中的特殊字符转义，使其可以安全地嵌入到 C# 字符串字面量中。
    /// </summary>
    /// <param name="value">要转义的字符串。</param>
    /// <returns>转义后的字符串。</returns>
    public static string escape_for_string(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var sb = new StringBuilder(value.Length);

        foreach (var c in value)
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }

        return sb.ToString();
    }
}