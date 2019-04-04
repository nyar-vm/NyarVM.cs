namespace Std.Data.Text.Notedown;

/// <summary>
///     解析选项配置，对齐 pandoc ReaderOptions
/// </summary>
public sealed class ParseOptions
{
    /// <summary>
    ///     编码提示，如 "utf-8"、"gbk"
    /// </summary>
    public string encoding_hint { get; init; } = "utf-8";

    /// <summary>
    ///     是否提取元数据
    /// </summary>
    public bool extract_metadata { get; init; } = true;
}