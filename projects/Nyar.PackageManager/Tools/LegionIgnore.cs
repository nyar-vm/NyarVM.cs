using System.Text.RegularExpressions;

namespace Nyar.PackageManager.Tools;

public class LegionIgnore
{
    private readonly string _file_path;
    private readonly List<string> _patterns = [];

    public LegionIgnore(string directoryPath)
    {
        _file_path = Path.Combine(directoryPath, "legion.ignore");
        load();
    }

    public void load()
    {
        _patterns.Clear();

        if (!File.Exists(_file_path)) return;

        var lines = File.ReadAllLines(_file_path);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

            _patterns.Add(trimmed);
        }
    }

    public void save()
    {
        File.WriteAllLines(_file_path, _patterns);
    }

    public bool is_ignored(string path)
    {
        var normalizedPath = path.Replace("\\", "/");
        var fileName = Path.GetFileName(normalizedPath);

        foreach (var pattern in _patterns)
            if (matches_pattern(normalizedPath, fileName, pattern))
                return true;

        return false;
    }

    public void add_pattern(string pattern)
    {
        if (!_patterns.Contains(pattern)) _patterns.Add(pattern);
    }

    public void remove_pattern(string pattern)
    {
        _patterns.Remove(pattern);
    }

    public List<string> get_patterns()
    {
        return [.. _patterns];
    }

    public bool exists()
    {
        return File.Exists(_file_path);
    }

    private bool matches_pattern(string path, string fileName, string pattern)
    {
        var negated = pattern.StartsWith("!");
        if (negated) pattern = pattern[1..];

        var directoryOnly = pattern.EndsWith("/");
        if (directoryOnly) pattern = pattern.TrimEnd('/');

        var matches = is_match(path, fileName, pattern, directoryOnly);

        return negated ? !matches : matches;
    }

    private bool is_match(string path, string fileName, string pattern, bool directoryOnly)
    {
        // 简单文件名匹配（如 *.log）
        if (pattern.StartsWith("*."))
        {
            var ext = pattern[1..];
            if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return true;
        }

        // 精确文件名匹配
        if (!pattern.Contains("/") && !pattern.Contains("*") && !pattern.Contains("?"))
        {
            if (string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase)) return true;

            // 目录名匹配
            if (directoryOnly)
            {
                var parts = path.Split('/');
                foreach (var part in parts)
                    if (string.Equals(part, pattern, StringComparison.OrdinalIgnoreCase))
                        return true;
            }

            return false;
        }

        // 通配符匹配
        var regexPattern = convert_to_regex(pattern);

        try
        {
            if (Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase)) return true;

            if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase)) return true;
        }
        catch
        {
            // 正则表达式无效时回退到简单匹配
        }

        return false;
    }

    private string convert_to_regex(string pattern)
    {
        var regex = pattern;

        // 转义正则特殊字符（先转义反斜杠）
        regex = regex.Replace("\\", "\\\\");
        regex = regex.Replace(".", "\\.");
        regex = regex.Replace("+", "\\+");
        regex = regex.Replace("(", "\\(");
        regex = regex.Replace(")", "\\)");
        regex = regex.Replace("[", "\\[");
        regex = regex.Replace("]", "\\]");
        regex = regex.Replace("{", "\\{");
        regex = regex.Replace("}", "\\}");
        regex = regex.Replace("^", "\\^");
        regex = regex.Replace("$", "\\$");

        // 处理通配符（按从长到短的顺序）
        regex = regex.Replace("/**/", "/(.*/)?");
        regex = regex.Replace("**", ".*");
        regex = regex.Replace("*", "[^/]*");
        regex = regex.Replace("?", ".");

        // 锚定
        if (!pattern.StartsWith("/") && !pattern.StartsWith("*"))
            regex = "(^|.*/)" + regex;
        else if (pattern.StartsWith("/")) regex = "^" + regex[1..];

        return regex + "$";
    }

    public static LegionIgnore create_default(string directoryPath)
    {
        var ignore = new LegionIgnore(directoryPath);

        var defaultPatterns = new[]
        {
            "# Legion ignore file",
            "# Patterns follow gitignore syntax",
            "",
            "# Build outputs",
            "build/",
            "dist/",
            "bin/",
            "obj/",
            "",
            "# Dependencies (managed by Legion)",
            "vendors/",
            "",
            "# Cache",
            ".cache/",
            "",
            "# IDE",
            ".idea/",
            ".vscode/",
            "*.user",
            "",
            "# OS files",
            ".DS_Store",
            "Thumbs.db",
            "",
            "# Logs",
            "*.log",
            "logs/",
            "",
            "# Test outputs",
            "coverage/",
            ".nyc_output/",
            "",
            "# Legion internal",
            ".valkyrie/",
            "legion-lock.von"
        };

        ignore._patterns.AddRange(defaultPatterns.Where(p => !string.IsNullOrEmpty(p) && !p.StartsWith("#")));
        ignore.save();

        return ignore;
    }
}