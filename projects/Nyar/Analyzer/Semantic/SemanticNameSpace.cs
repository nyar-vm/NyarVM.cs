namespace Nyar.Analyzer.Semantic;

/// <summary>
///     HIR 内部使用的命名空间路径。
///     内部以分段数组表示，`∷` 只用于规范打印，不参与业务判定。
/// </summary>
public record SemanticNameSpace
{
    public static readonly SemanticNameSpace empty = new([]);

    /// <summary>
    ///     HIR 内部使用的命名空间路径。
    ///     内部以分段数组表示，`∷` 只用于规范打印，不参与业务判定。
    /// </summary>
    public SemanticNameSpace(IReadOnlyList<string> parts)
    {
        this.parts = parts;
    }

    /// <summary>
    ///     命名空间的结构化分段。
    /// </summary>
    public IReadOnlyList<string> parts { get; init; }

    /// <summary>
    ///     判断当前命名空间是否为空。
    /// </summary>
    public bool is_empty => parts.Count == 0 || parts.All(string.IsNullOrWhiteSpace);

    /// <summary>
    ///     由当前命名空间实例创建同语义层的命名空间。
    /// </summary>
    protected internal virtual SemanticNameSpace create_name_space(IReadOnlyList<string> newParts)
    {
        return new SemanticNameSpace(newParts);
    }

    /// <summary>
    ///     由当前命名空间实例创建同语义层的名称路径。
    /// </summary>
    public virtual SemanticNamePath create_name_path(IReadOnlyList<string> newParts)
    {
        return new SemanticNamePath(newParts);
    }

    /// <summary>
    ///     判断两个命名空间路径是否语义相等。
    /// </summary>
    public bool semantically_equals(SemanticNameSpace? other)
    {
        if (other is null || parts.Count != other.parts.Count) return false;

        return !parts.Where((t, index) => !string.Equals(t, other.parts[index], StringComparison.Ordinal)).Any();
    }

    /// <summary>
    ///     将当前命名空间与声明的子命名空间合并。
    /// </summary>
    public SemanticNameSpace combine(SemanticNameSpace? declaredNamespace)
    {
        var declaredPath = declaredNamespace ?? empty;
        if (is_empty) return declaredPath;

        return declaredPath.is_empty
            ? this
            : create_name_space([.. parts, .. declaredPath.parts]);
    }

    /// <summary>
    ///     将当前命名空间与成员名组合为完整名称路径。
    /// </summary>
    public SemanticNamePath qualify(SemanticNamePath? memberName)
    {
        var namePath = memberName ?? create_name_path([]);
        if (is_empty) return namePath;

        return namePath.is_empty
            ? create_name_path([.. parts])
            : namePath.create_name_path([.. parts, .. namePath.parts]);
    }
}