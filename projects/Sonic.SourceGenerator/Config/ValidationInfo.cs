namespace Sonic.Data.Generator.Config;

/// <summary>
///     配置属性的验证特性信息，编译期从符号提取。
/// </summary>
internal readonly record struct ValidationInfo
{
    /// <summary>
    ///     验证特性类型。
    /// </summary>
    public readonly ValidationKind kind;

    /// <summary>
    ///     [Range] 的最大值，仅当 <see cref="kind" /> 为 <see cref="ValidationKind.range" /> 时有效。
    /// </summary>
    public readonly double range_max;

    /// <summary>
    ///     [Range] 的最小值，仅当 <see cref="kind" /> 为 <see cref="ValidationKind.range" /> 时有效。
    /// </summary>
    public readonly double range_min;

    /// <summary>
    ///     [Regex] 的正则表达式模式，仅当 <see cref="kind" /> 为 <see cref="ValidationKind.regex" /> 时有效。
    /// </summary>
    public readonly string? regex_pattern;

    /// <summary>
    ///     使用验证类型初始化 <see cref="ValidationInfo" /> 的新实例。
    /// </summary>
    public ValidationInfo(ValidationKind kind)
    {
        this.kind = kind;
        range_min = 0;
        range_max = 0;
        regex_pattern = null;
    }

    /// <summary>
    ///     使用 [Range] 参数初始化 <see cref="ValidationInfo" /> 的新实例。
    /// </summary>
    public ValidationInfo(double min, double max)
    {
        kind = ValidationKind.range;
        range_min = min;
        range_max = max;
        regex_pattern = null;
    }

    /// <summary>
    ///     使用 [Regex] 模式初始化 <see cref="ValidationInfo" /> 的新实例。
    /// </summary>
    public ValidationInfo(string pattern)
    {
        kind = ValidationKind.regex;
        range_min = 0;
        range_max = 0;
        regex_pattern = pattern;
    }
}