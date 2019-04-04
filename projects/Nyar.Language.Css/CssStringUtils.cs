using System.Globalization;

namespace Nyar.Language.Css;

/// <summary>
///     CSS 相关的字符串工具方法
/// </summary>
public static class CssStringUtils
{
    /// <summary>
    ///     检查字符串是否为 JavaScript 保留关键字
    /// </summary>
    public static bool is_js_keyword(string s)
    {
        return s is "if" or "else" or "for" or "while" or "do" or "switch" or "case" or "break"
            or "continue" or "return" or "function" or "var" or "let" or "const" or "class"
            or "new" or "typeof" or "instanceof" or "void" or "delete" or "in" or "of"
            or "try" or "catch" or "finally" or "throw" or "async" or "await" or "yield"
            or "import" or "export" or "default" or "from" or "true" or "false" or "null"
            or "undefined" or "micro";
    }

    /// <summary>
    ///     检查字符串是否为字面量值（布尔、数字、字符串）
    /// </summary>
    public static bool is_literal(string s)
    {
        return s is "true" or "false" or "null" or "undefined" or "NaN" or "Infinity"
               || double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
               || (s.StartsWith("\"") && s.EndsWith("\""))
               || (s.StartsWith("'") && s.EndsWith("'"))
               || (s.StartsWith("`") && s.EndsWith("`"));
    }

    /// <summary>
    ///     将 PascalCase 或 camelCase 名称转换为 kebab-case
    /// </summary>
    public static string to_kebab_case(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var sb = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}