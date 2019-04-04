using System.Text;

namespace Std.Document;

/// <summary>
///     格式检测器，通过文件扩展名和内容特征自动检测源格式
/// </summary>
public static class FormatDetector
{
    private static readonly Dictionary<string, SourceFormat> _extension_map = new(StringComparer.OrdinalIgnoreCase)
    {
        [".json"] = SourceFormat.json,
        [".csv"] = SourceFormat.csv,
        [".tsv"] = SourceFormat.csv,
        [".yaml"] = SourceFormat.yaml,
        [".yml"] = SourceFormat.yaml,
        [".toml"] = SourceFormat.toml,
        [".ini"] = SourceFormat.ini,
        [".cfg"] = SourceFormat.ini,
        [".conf"] = SourceFormat.ini,
        [".md"] = SourceFormat.markdown,
        [".markdown"] = SourceFormat.markdown,
        [".notedown"] = SourceFormat.notedown,
        [".html"] = SourceFormat.html,
        [".htm"] = SourceFormat.html,
        [".xml"] = SourceFormat.xml,
        [".txt"] = SourceFormat.plain_text
    };

    /// <summary>
    ///     根据文件路径检测格式（优先扩展名）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>检测到的源格式</returns>
    public static SourceFormat detect(string filePath)
    {
        var ext = Path.GetExtension(filePath);

        if (_extension_map.TryGetValue(ext, out var format)) return format;

        return SourceFormat.plain_text;
    }

    /// <summary>
    ///     根据文件内容特征检测格式
    /// </summary>
    /// <param name="content">文件内容</param>
    /// <returns>检测到的源格式</returns>
    public static SourceFormat detect(byte[] content)
    {
        if (content.Length == 0) return SourceFormat.plain_text;

        var text = Encoding.UTF8.GetString(content).TrimStart();

        if (text.StartsWith('{') || text.StartsWith('[')) return SourceFormat.json;

        if (text.StartsWith("---")) return SourceFormat.yaml;

        if (text.StartsWith('<'))
        {
            if (text.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                return SourceFormat.html;

            if (text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)) return SourceFormat.xml;

            return SourceFormat.html;
        }

        if (text.StartsWith('[') && text.Contains(']')) return SourceFormat.ini;

        if (text.StartsWith('#')) return SourceFormat.markdown;

        return SourceFormat.plain_text;
    }

    /// <summary>
    ///     获取源格式对应的文件扩展名
    /// </summary>
    /// <param name="format">源格式</param>
    /// <returns>默认文件扩展名</returns>
    public static string get_extension(SourceFormat format)
    {
        return format switch
        {
            SourceFormat.json => ".json",
            SourceFormat.csv => ".csv",
            SourceFormat.yaml => ".yaml",
            SourceFormat.toml => ".toml",
            SourceFormat.ini => ".ini",
            SourceFormat.markdown => ".md",
            SourceFormat.notedown => ".notedown",
            SourceFormat.html => ".html",
            SourceFormat.xml => ".xml",
            SourceFormat.plain_text => ".txt",
            _ => ".txt"
        };
    }

    /// <summary>
    ///     获取目标格式对应的文件扩展名
    /// </summary>
    /// <param name="format">目标格式</param>
    /// <returns>默认文件扩展名</returns>
    public static string get_extension(TargetFormat format)
    {
        return format switch
        {
            TargetFormat.json => ".json",
            TargetFormat.csv => ".csv",
            TargetFormat.yaml => ".yaml",
            TargetFormat.toml => ".toml",
            TargetFormat.ini => ".ini",
            TargetFormat.markdown => ".md",
            TargetFormat.notedown => ".notedown",
            TargetFormat.html => ".html",
            TargetFormat.xml => ".xml",
            TargetFormat.plain_text => ".txt",
            _ => ".txt"
        };
    }
}