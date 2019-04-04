namespace Std.Command.Migration;

/// <summary>
///     单个迁移对照示例
/// </summary>
public sealed class MigrationExample
{
    /// <summary>
    ///     示例标题
    /// </summary>
    public string title { get; init; } = string.Empty;

    /// <summary>
    ///     迁移前使用的框架名称
    /// </summary>
    public string before_framework { get; init; } = string.Empty;

    /// <summary>
    ///     迁移前代码
    /// </summary>
    public string before_code { get; init; } = string.Empty;

    /// <summary>
    ///     Iris 等效代码
    /// </summary>
    public string after_code { get; init; } = string.Empty;
}