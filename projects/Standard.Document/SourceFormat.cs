namespace Std.Document;

/// <summary>
///     源格式枚举，覆盖常见输入文档格式
/// </summary>
public enum SourceFormat
{
    /// <summary>自动检测</summary>
    auto = 0,

    /// <summary>JSON</summary>
    json = 1,

    /// <summary>CSV</summary>
    csv = 2,

    /// <summary>YAML</summary>
    yaml = 3,

    /// <summary>TOML</summary>
    toml = 4,

    /// <summary>INI</summary>
    ini = 5,

    /// <summary>Markdown</summary>
    markdown = 6,

    /// <summary>Notedown（统一文档格式）</summary>
    notedown = 7,

    /// <summary>HTML</summary>
    html = 8,

    /// <summary>XML</summary>
    xml = 9,

    /// <summary>纯文本</summary>
    plain_text = 10
}