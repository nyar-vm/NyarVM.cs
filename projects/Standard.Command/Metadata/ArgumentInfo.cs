namespace Std.Command.Metadata;

/// <summary>
///     命令位置参数元数据信息
/// </summary>
public sealed class ArgumentInfo
{
    /// <summary>
    ///     参数名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     描述
    /// </summary>
    public string description { get; init; } = string.Empty;

    /// <summary>
    ///     是否必填
    /// </summary>
    public bool required { get; init; }
}