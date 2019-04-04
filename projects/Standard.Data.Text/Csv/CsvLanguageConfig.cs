namespace Std.Data.Text.Csv;

/// <summary>
///     CSV 语言配置
/// </summary>
public sealed class CsvLanguageConfig
{
    /// <summary>
    ///     字段分隔符（默认逗号）
    /// </summary>
    public char delimiter { get; init; } = ',';

    /// <summary>
    ///     是否将第一行作为表头
    /// </summary>
    public bool has_header { get; init; } = true;

    /// <summary>
    ///     默认配置实例
    /// </summary>
    public static CsvLanguageConfig @default { get; } = new();
}