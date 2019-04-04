namespace Std.Template.Common;

public static class FrontMatterParser
{
    public static (string? frontMatter, string content) Extract(string content)
    {
        if (!content.StartsWith("---")) return (null, content);

        var endIndex = content.IndexOf("---", 3);
        if (endIndex <= 0) return (null, content);

        var frontMatter = content[3..endIndex].Trim();
        var remaining = content[(endIndex + 3)..].Trim();
        return (frontMatter, remaining);
    }

    public static YamlMapping? Parse(string? frontMatter)
    {
        if (string.IsNullOrWhiteSpace(frontMatter)) return null;

        var parser = new YamlParser();
        var result = parser.Parse(frontMatter);
        if (!result.Success || result.Value is not YamlMapping mapping) return null;

        return mapping;
    }
}