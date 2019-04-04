namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 词法单元
/// </summary>
public readonly struct YamlToken
{
    /// <summary>
    ///     词法单元类型
    /// </summary>
    public YamlTokenType type { get; }


    /// <summary>
    ///     原始文本
    /// </summary>
    public string text { get; }


    /// <summary>
    ///     行号（从 1 开始）
    /// </summary>
    public int line { get; }


    /// <summary>
    ///     列号（从 1 开始）
    /// </summary>
    public int column { get; }


    /// <summary>
    ///     缩进级别
    /// </summary>
    public int indent { get; }

    public YamlToken(YamlTokenType type, string text, int line, int column, int indent = 0)
    {
        this.type = type;
        this.text = text;
        this.line = line;
        this.column = column;
        this.indent = indent;
    }

    public override string ToString()
    {
        return $"{type}('{text}') @ {line}:{column} indent={indent}";
    }
}