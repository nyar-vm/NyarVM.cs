namespace Std.Data.Text.Awsl;

/// <summary>
///     Awsl 函数声明
/// </summary>
public sealed class AwslMethod
{
    /// <summary>
    ///     函数名
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     参数列表（逗号分隔的原始字符串）
    /// </summary>
    public string parameters { get; init; } = string.Empty;

    /// <summary>
    ///     函数体（原始字符串）
    /// </summary>
    public string body { get; init; } = string.Empty;

    /// <summary>
    ///     是否为微函数（编译到 WASM）
    /// </summary>
    public bool is_micro { get; init; }
}