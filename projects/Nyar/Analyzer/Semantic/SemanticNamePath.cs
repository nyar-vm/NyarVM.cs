namespace Nyar.Analyzer.Semantic;

/// <summary>
///     HIR 内部使用的类型名称路径。
///     内部以分段数组表示，`∷` 只用于规范打印，不参与业务判定。
/// </summary>
public record SemanticNamePath
{
    public static readonly SemanticNamePath empty = new([]);

    /// <summary>
    ///     HIR 内部使用的类型名称路径。
    ///     内部以分段数组表示，`∷` 只用于规范打印，不参与业务判定。
    /// </summary>
    public SemanticNamePath(IReadOnlyList<string> parts)
    {
        this.parts = parts;
    }

    public SemanticNameSpace @namespace => parts.Count > 1
        ? create_name_space([.. parts.Take(parts.Count - 1)])
        : create_name_space([]);

    public string name => parts.Count > 0 ? parts[^1] : string.Empty;

    public bool is_empty => parts.Count == 0 || parts.All(string.IsNullOrWhiteSpace);

    /// <summary>
    ///     名称的结构化分段。
    /// </summary>
    public IReadOnlyList<string> parts { get; init; }

    /// <summary>
    ///     由当前名称实例创建同语义层的名称路径。
    /// </summary>
    protected internal virtual SemanticNamePath create_name_path(IReadOnlyList<string> newParts)
    {
        return new SemanticNamePath(newParts);
    }

    /// <summary>
    ///     由当前名称实例创建同语义层的命名空间。
    /// </summary>
    protected internal virtual SemanticNameSpace create_name_space(IReadOnlyList<string> newParts)
    {
        return new SemanticNameSpace(newParts);
    }

    public bool semantically_equals(SemanticNamePath? other)
    {
        if (other is null || parts.Count != other.parts.Count) return false;

        for (var i = 0; i < parts.Count; i++)
            if (!string.Equals(parts[i], other.parts[i], StringComparison.Ordinal))
                return false;

        return true;
    }

    /// <summary>
    ///     使用结构化命名空间与宿主名称路径拼接限定名。
    /// </summary>
    public static SemanticNamePath qualify(
        SemanticNameSpace? currentNamespace,
        SemanticNamePath? ownerName,
        SemanticNamePath? memberName
    )
    {
        var parts = new List<string>();
        if (currentNamespace is not null) parts.AddRange(currentNamespace.parts);

        if (ownerName is not null) parts.AddRange(ownerName.parts);

        if (memberName is not null) parts.AddRange(memberName.parts);
        if (memberName is not null) return memberName.create_name_path(parts);

        if (ownerName is not null) return ownerName.create_name_path(parts);

        if (currentNamespace is not null) return currentNamespace.create_name_path(parts);

        return new SemanticNamePath(parts);
    }

    public static bool matches(
        SemanticNamePath simplePath,
        SemanticNamePath qualifiedPath,
        SemanticNamePath queryPath,
        SemanticNameSpace? currentNamespace = null
    )
    {
        var qualifiedQueryPath = currentNamespace is null or { is_empty: true }
            ? queryPath
            : qualify(currentNamespace, null, queryPath);

        return simplePath.semantically_equals(queryPath)
               || qualifiedPath.semantically_equals(queryPath)
               || qualifiedPath.semantically_equals(qualifiedQueryPath)
               || (queryPath.parts.Count > 0 &&
                   qualifiedPath.parts.Count > queryPath.parts.Count &&
                   qualifiedPath.parts.Skip(qualifiedPath.parts.Count - queryPath.parts.Count)
                       .SequenceEqual(queryPath.parts));
    }
}