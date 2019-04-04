using Nyar.Analyzer.Semantic;

namespace Nyar.Language.Valkyrie.Semantic;

/// <summary>
///     Valkyrie 命名空间。
///     负责 Valkyrie 文本与结构化命名空间之间的适配。
/// </summary>
public record ValkyrieNameSpace(IReadOnlyList<string> parts) : SemanticNameSpace(parts)
{
    public new static readonly ValkyrieNameSpace empty = new([]);

    /// <summary>
    ///     从 Valkyrie 名称文本解析命名空间。
    /// </summary>
    public static ValkyrieNameSpace parse(string? rawName)
    {
        return new ValkyrieNameSpace(split_parts(rawName));
    }

    /// <summary>
    ///     将结构化命名空间渲染为 Valkyrie 文本。
    /// </summary>
    public override string ToString()
    {
        return string.Join("∷", parts);
    }

    /// <summary>
    ///     由当前命名空间实例创建 Valkyrie 命名空间。
    /// </summary>
    protected internal override SemanticNameSpace create_name_space(IReadOnlyList<string> newParts)
    {
        return new ValkyrieNameSpace(newParts);
    }

    /// <summary>
    ///     由当前命名空间实例创建 Valkyrie 名称路径。
    /// </summary>
    protected internal override SemanticNamePath create_name_path(IReadOnlyList<string> newParts)
    {
        return new ValkyrieNamePath(newParts);
    }

    private static IReadOnlyList<string> split_parts(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return [];
        }

        var normalized = rawName
            .Replace("::", "∷", StringComparison.Ordinal)
            .Replace(".", "∷", StringComparison.Ordinal);

        return normalized
            .Split('∷', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();
    }
}
