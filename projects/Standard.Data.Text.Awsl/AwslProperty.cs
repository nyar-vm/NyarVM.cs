namespace Std.Data.Text.Awsl;

/// <summary>
///     Awsl 属性声明
/// </summary>
public sealed class AwslProperty
{
    /// <summary>
    ///     属性名
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     类型名
    /// </summary>
    public string type_name { get; init; } = "auto";

    /// <summary>
    ///     是否只读
    /// </summary>
    public bool is_readonly { get; init; }

    /// <summary>
    ///     默认值（原始字符串）
    /// </summary>
    public string? default_value { get; init; }

    /// <summary>
    ///     默认值种类
    /// </summary>
    public AwslValueKind default_value_kind { get; init; }
}