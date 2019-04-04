namespace Core.Config;

/// <summary>
///     属性名到配置键名的命名约定，控制自动转换规则。
/// </summary>
public enum NamingConvention
{
    /// <summary>
    ///     camelCase 风格。
    /// </summary>
    camel_case = 0,

    /// <summary>
    ///     PascalCase 风格。
    /// </summary>
    pascal_case = 1,

    /// <summary>
    ///     snake_case 风格。
    /// </summary>
    snake_case = 2,

    /// <summary>
    ///     kebab-case 风格。
    /// </summary>
    kebab_case = 3,

    /// <summary>
    ///     全小写风格。
    /// </summary>
    lower_case = 4,

    /// <summary>
    ///     全大写风格。
    /// </summary>
    upper_case = 5
}