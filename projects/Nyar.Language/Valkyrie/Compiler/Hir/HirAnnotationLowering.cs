using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Term;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

public static class HirAnnotationLowering
{
    private static readonly HashSet<string> _builtin_annotations = new(StringComparer.Ordinal)
    {
        "main", "intrinsic", "infix", "prefix", "postfix", "virtual",
        "public", "private", "protected", "internal", "export"
    };

    public static IReadOnlyList<HirAttribute> build_annotations(Annotations annotations,
        string? currentNamespace = null)
    {
        var lowered = new List<HirAttribute>();

        foreach (var modifier in annotations.modifiers)
            lowered.Add(new HirAttribute(resolve_annotation_name(modifier.name, currentNamespace), []));

        foreach (var attribute in annotations.attributes())
            lowered.Add(new HirAttribute(
                resolve_annotation_name(attribute.name, currentNamespace),
                attribute.arguments?.items.Select(argument => extract_text_value(argument.value!)).ToArray() ??
                []));

        return lowered;
    }

    private static string extract_text_value(TermArgumentItem argument)
    {
        return extract_text_value(argument.value);
    }

    private static string extract_text_value(TermNode? value)
    {
        if (value is TermLiteralTextNode textNode) return textNode.value;

        return value?.ToString() ?? string.Empty;
    }

    private static string resolve_annotation_name(string rawName, string? currentNamespace)
    {
        var normalized = rawName.Replace("::", ".", StringComparison.Ordinal).Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;

        if (_builtin_annotations.Contains(normalized) || normalized.Contains('.', StringComparison.Ordinal))
            return normalized;

        return string.IsNullOrWhiteSpace(currentNamespace) ? normalized : $"{currentNamespace}.{normalized}";
    }
}