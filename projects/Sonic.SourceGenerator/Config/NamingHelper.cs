using System.Text;

namespace Sonic.Data.Generator.Config;

/// <summary>
///     命名约定转换工具，将 PascalCase 属性名按指定约定转换为配置键名。
/// </summary>
internal static class NamingHelper
{
    /// <summary>
    ///     将 PascalCase 名称按指定命名约定转换为配置键名。
    /// </summary>
    /// <param name="name">PascalCase 格式的属性名。</param>
    /// <param name="convention">目标命名约定。</param>
    /// <returns>转换后的配置键名。</returns>
    public static string apply_convention(string name, NamingConvention convention)
    {
        if (string.IsNullOrEmpty(name)) return name;

        switch (convention)
        {
            case NamingConvention.camel_case:
            {
                return to_camel_case(name);
            }
            case NamingConvention.pascal_case:
            {
                return name;
            }
            case NamingConvention.snake_case:
            {
                return to_snake_case(name);
            }
            case NamingConvention.kebab_case:
            {
                return to_kebab_case(name);
            }
            case NamingConvention.lower_case:
            {
                return name.ToLowerInvariant();
            }
            case NamingConvention.upper_case:
            {
                return name.ToUpperInvariant();
            }
            default:
            {
                return to_camel_case(name);
            }
        }
    }

    /// <summary>
    ///     将类名转换为默认的 camelCase 节名。
    /// </summary>
    /// <param name="className">类名。</param>
    /// <returns>camelCase 格式的节名。</returns>
    public static string to_section_name(string className)
    {
        return to_camel_case(className);
    }

    /// <summary>
    ///     PascalCase → camelCase，如 TimeoutSeconds → timeoutSeconds。
    /// </summary>
    private static string to_camel_case(string name)
    {
        if (name.Length == 0) return name;

        if (name.Length == 1) return name.ToLowerInvariant();

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>
    ///     PascalCase → snake_case，如 TimeoutSeconds → timeout_seconds。
    /// </summary>
    private static string to_snake_case(string name)
    {
        return insert_separator(name, '_').ToLowerInvariant();
    }

    /// <summary>
    ///     PascalCase → kebab-case，如 TimeoutSeconds → timeout-seconds。
    /// </summary>
    private static string to_kebab_case(string name)
    {
        return insert_separator(name, '-').ToLowerInvariant();
    }

    /// <summary>
    ///     在大写字母前插入分隔符，用于 snake_case 和 kebab-case 转换。
    ///     连续大写字母视为一个词（如 XMLParser → xml_parser）。
    /// </summary>
    private static string insert_separator(string name, char separator)
    {
        var sb = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    var prev = name[i - 1];

                    if (char.IsLower(prev) || (i + 1 < name.Length && char.IsLower(name[i + 1]))) sb.Append(separator);
                }

                sb.Append(c);
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}