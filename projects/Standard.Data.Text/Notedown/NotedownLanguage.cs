using Std.Data.Text.Notedown.Parsing;
using Std.Data.Text.Notedown.Syntax;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Notedown;

/// <summary>
///     Notedown 语言前端，封装解析、格式化管线和配置
///     Notedown 是统一文档语言，其 AST 即为所有格式的中间表示
/// </summary>
public sealed class NotedownLanguage : Language
{
    private readonly NotedownFormatter _formatter;
    private readonly NotedownParser _parser;

    /// <summary>
    ///     创建 Notedown 语言实例（使用默认配置）
    /// </summary>
    public NotedownLanguage()
        : this(NotedownLanguageConfig.@default)
    {
    }

    /// <summary>
    ///     创建 Notedown 语言实例
    /// </summary>
    public NotedownLanguage(NotedownLanguageConfig config)
    {
        this.config = config;
        _parser = new NotedownParser();
        _formatter = new NotedownFormatter(config);
    }

    /// <inheritdoc />
    public override string name => "Notedown";

    /// <summary>
    ///     语言配置
    /// </summary>
    public NotedownLanguageConfig config { get; }

    /// <inheritdoc />
    public override NotedownDocument to_notedown(object ast)
    {
        return (NotedownDocument)ast;
    }

    /// <inheritdoc />
    public override object from_notedown(NotedownDocument document)
    {
        return format(document);
    }

    /// <summary>
    ///     解析 Notedown 文本为文档 AST
    /// </summary>
    public NotedownDocument parse(string source)
    {
        return _parser.parse(source);
    }

    /// <summary>
    ///     将 Notedown 文档格式化为 Notedown 文本
    /// </summary>
    public string format(NotedownDocument document)
    {
        return _formatter.format(document);
    }
}