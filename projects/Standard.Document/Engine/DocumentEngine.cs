using System.Text;
using Std.Data.Text.Csv;
using Std.Data.Text.Ini;
using Std.Data.Text.Json;
using Std.Data.Text.Markdown;
using Std.Data.Text.Notedown;
using Std.Data.Text.Notedown.Syntax;
using Std.Data.Text.Toml;
using Std.Data.Text.Xml;
using Std.Data.Text.Yaml;

namespace Std.Document.Engine;

/// <summary>
///     Argus 全视文档引擎，对标 pandoc 的通用文档编排入口
///     核心语言为 Notedown，所有格式转换统一经过 NotedownDocument AST
///     通过各格式的 Language 实例直接调用 ToNotedown / FromNotedown 方法
/// </summary>
public sealed class DocumentEngine
{
    private readonly CsvLanguage _csv;
    private readonly IniLanguage _ini;
    private readonly JsonLanguage _json;
    private readonly MarkdownLanguage _markdown;
    private readonly NotedownLanguage _notedown;
    private readonly TomlLanguage _toml;
    private readonly XmlLanguage _xml;
    private readonly YamlLanguage _yaml;

    /// <summary>
    ///     创建 ArgusEngine 实例（使用默认配置）
    /// </summary>
    public DocumentEngine()
        : this(NotedownLanguageConfig.@default,
            MarkdownLanguageConfig.@default,
            CsvLanguageConfig.@default,
            XmlLanguageConfig.@default)
    {
    }

    /// <summary>
    ///     创建 ArgusEngine 实例（自定义 Notedown 配置）
    /// </summary>
    public DocumentEngine(NotedownLanguageConfig notedownConfig)
    {
        _notedown = new NotedownLanguage(notedownConfig);
        _markdown = new MarkdownLanguage();
        _json = new JsonLanguage();
        _csv = new CsvLanguage();
        _yaml = new YamlLanguage();
        _toml = new TomlLanguage();
        _ini = new IniLanguage();
        _xml = new XmlLanguage();
    }

    /// <summary>
    ///     创建 ArgusEngine 实例（完整自定义配置）
    /// </summary>
    public DocumentEngine(
        NotedownLanguageConfig notedownConfig,
        MarkdownLanguageConfig markdownConfig,
        CsvLanguageConfig csvConfig,
        XmlLanguageConfig xmlConfig)
    {
        _notedown = new NotedownLanguage(notedownConfig);
        _markdown = new MarkdownLanguage(markdownConfig);
        _json = new JsonLanguage();
        _csv = new CsvLanguage(csvConfig);
        _yaml = new YamlLanguage();
        _toml = new TomlLanguage();
        _ini = new IniLanguage();
        _xml = new XmlLanguage(xmlConfig);
    }

    #region 格式推断

    private static TargetFormat infer_target_format(string outputPath)
    {
        var ext = Path.GetExtension(outputPath).ToLowerInvariant();
        return ext switch
        {
            ".json" => TargetFormat.json,
            ".csv" => TargetFormat.csv,
            ".tsv" => TargetFormat.csv,
            ".yaml" or ".yml" => TargetFormat.yaml,
            ".toml" => TargetFormat.toml,
            ".ini" or ".cfg" or ".conf" => TargetFormat.ini,
            ".md" or ".markdown" => TargetFormat.markdown,
            ".notedown" => TargetFormat.notedown,
            ".html" or ".htm" => TargetFormat.html,
            ".xml" => TargetFormat.xml,
            ".txt" => TargetFormat.plain_text,
            _ => TargetFormat.plain_text
        };
    }

    #endregion

    #region 核心转换管线

    /// <summary>
    ///     从文件读取并解析为 NotedownDocument，自动检测格式
    /// </summary>
    public NotedownDocument read(string inputPath, SourceFormat? formatHint = null)
    {
        var format = formatHint ?? FormatDetector.detect(inputPath);
        var source = File.ReadAllText(inputPath);
        return read(source, format);
    }

    /// <summary>
    ///     从文本源解析为 NotedownDocument
    /// </summary>
    public NotedownDocument read(string source, SourceFormat format)
    {
        if (format == SourceFormat.auto) format = FormatDetector.detect(Encoding.UTF8.GetBytes(source));

        return format switch
        {
            SourceFormat.notedown => _notedown.parse(source),
            SourceFormat.json => _json.to_notedown(source),
            SourceFormat.csv => _csv.to_notedown(source),
            SourceFormat.markdown => _markdown.to_notedown(_markdown.parse(source)),
            SourceFormat.yaml => _yaml.to_notedown(_yaml.parse(source)),
            SourceFormat.toml => _toml.to_notedown(_toml.parse(source)!),
            SourceFormat.ini => _ini.to_notedown(_ini.parse(source)),
            SourceFormat.plain_text => parse_plain_text(source),
            SourceFormat.html => throw new NotSupportedException("HTML 读取器尚未实现"),
            SourceFormat.xml => _xml.to_notedown(source),
            _ => throw new NotSupportedException($"不支持的源格式: {format}")
        };
    }

    /// <summary>
    ///     将 NotedownDocument 写入文件
    /// </summary>
    public void write(NotedownDocument document, string outputPath, TargetFormat format)
    {
        var text = write_to_string(document, format);
        File.WriteAllText(outputPath, text);
    }

    /// <summary>
    ///     将 NotedownDocument 格式化为目标格式文本
    /// </summary>
    public string write_to_string(NotedownDocument document, TargetFormat format)
    {
        return format switch
        {
            TargetFormat.notedown => _notedown.format(document),
            TargetFormat.json => (string)_json.from_notedown(document),
            TargetFormat.csv => (string)_csv.from_notedown(document),
            TargetFormat.markdown => (string)_markdown.from_notedown(document),
            TargetFormat.yaml => (string)_yaml.from_notedown(document),
            TargetFormat.toml => (string)_toml.from_notedown(document),
            TargetFormat.ini => (string)_ini.from_notedown(document),
            TargetFormat.plain_text => format_plain_text(document),
            TargetFormat.html => throw new NotSupportedException("HTML 写入器尚未实现"),
            TargetFormat.xml => (string)_xml.from_notedown(document),
            _ => throw new NotSupportedException($"不支持的目标格式: {format}")
        };
    }

    /// <summary>
    ///     文件到文件格式转换，自动检测源格式和推断目标格式
    /// </summary>
    public void convert(string inputPath, string outputPath,
        SourceFormat? sourceFormat = null, TargetFormat? targetFormat = null)
    {
        var srcFmt = sourceFormat ?? FormatDetector.detect(inputPath);
        var tgtFmt = targetFormat ?? infer_target_format(outputPath);

        var doc = read(inputPath, srcFmt);
        write(doc, outputPath, tgtFmt);
    }

    /// <summary>
    ///     流式格式转换：源文本 → 目标文本
    /// </summary>
    public string convert_to_string(string source, SourceFormat sourceFormat, TargetFormat targetFormat)
    {
        var doc = read(source, sourceFormat);
        return write_to_string(doc, targetFormat);
    }

    #endregion

    #region 文档创建辅助

    /// <summary>
    ///     创建空文档
    /// </summary>
    public static NotedownDocument create_document(IReadOnlyList<NotedownBlock> blocks)
    {
        return new NotedownDocument(blocks);
    }

    /// <summary>
    ///     创建带元数据的文档
    /// </summary>
    public static NotedownDocument create_document(Meta meta, IReadOnlyList<NotedownBlock> blocks)
    {
        return new NotedownDocument(meta, blocks);
    }

    #endregion

    #region 内置纯文本处理

    private static NotedownDocument parse_plain_text(string source)
    {
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var blocks = new List<NotedownBlock>();

        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;

            blocks.Add(new Para([new Str(line)]));
        }

        return new NotedownDocument(blocks);
    }

    private static string format_plain_text(NotedownDocument document)
    {
        var lines = new List<string>();

        foreach (var block in document.blocks)
        {
            var text = block_to_plain_text(block);
            if (!string.IsNullOrEmpty(text)) lines.Add(text);
        }

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string block_to_plain_text(NotedownBlock block)
    {
        return block switch
        {
            Para para => inlines_to_plain_text(para.inlines),
            Header header => inlines_to_plain_text(header.inlines),
            CodeBlock codeBlock => codeBlock.text,
            Plain plain => inlines_to_plain_text(plain.inlines),
            HorizontalRule => "---",
            Null => string.Empty,
            _ => string.Empty
        };
    }

    private static string inlines_to_plain_text(IReadOnlyList<NotedownInline> inlines)
    {
        var sb = new StringBuilder();
        foreach (var inline in inlines)
            switch (inline)
            {
                case Str str:
                    sb.Append(str.text);
                    break;
                case Space:
                    sb.Append(' ');
                    break;
                case SoftBreak:
                    sb.Append('\n');
                    break;
                case LineBreak:
                    sb.Append("\n\n");
                    break;
                case Code code:
                    sb.Append(code.text);
                    break;
                case Emph emph:
                    sb.Append(inlines_to_plain_text(emph.inlines));
                    break;
                case Strong strong:
                    sb.Append(inlines_to_plain_text(strong.inlines));
                    break;
                case Link link:
                    sb.Append(inlines_to_plain_text(link.inlines));
                    break;
            }

        return sb.ToString();
    }

    #endregion
}