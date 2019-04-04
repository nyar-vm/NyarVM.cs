namespace Std.Command.Metadata;

/// <summary>
///     命令选项元数据信息
/// </summary>
public sealed class OptionInfo
{
    /// <summary>
    ///     长名称
    /// </summary>
    public string long_name { get; init; } = string.Empty;

    /// <summary>
    ///     短名称
    /// </summary>
    public char? short_name { get; init; }

    /// <summary>
    ///     描述
    /// </summary>
    public string description { get; init; } = string.Empty;

    /// <summary>
    ///     是否为标志选项（bool 类型）
    /// </summary>
    public bool is_flag { get; init; }

    /// <summary>
    ///     别名列表
    /// </summary>
    public IReadOnlyList<string> aliases { get; init; } = [];
}