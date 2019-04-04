namespace Core.Data;

/// <summary>
///     字段重命名风格，控制序列化时的名称转换规则。
/// </summary>
public enum RenameStyle
{
    /// <summary>
    ///     不重命名。
    /// </summary>
    none = 0,

    /// <summary>
    ///     camelCase。
    /// </summary>
    camel_case = 1,

    /// <summary>
    ///     snake_case。
    /// </summary>
    snake_case = 2,

    /// <summary>
    ///     kebab-case。
    /// </summary>
    kebab_case = 3,

    /// <summary>
    ///     UpperCamelCase。
    /// </summary>
    upper_camel_case = 4,

    /// <summary>
    ///     全小写。
    /// </summary>
    lower_case = 5,

    /// <summary>
    ///     全大写。
    /// </summary>
    upper_case = 6
}