namespace Nyar.Analyzer.Semantic;

/// <summary>
///     类型变量，表示泛型推导过程中未解析的类型参数
/// </summary>
public sealed class TypeVariable : IType
{
    public TypeVariable(string name)
    {
        this.name = name;
    }

    /// <summary>
    ///     推导出的具体类型，未推导时为 null
    /// </summary>
    public IType? inferred_type { get; private set; }

    /// <summary>
    ///     是否已推导
    /// </summary>
    public bool is_resolved => inferred_type is not null;

    /// <summary>
    ///     获取最终类型（已推导则返回推导结果，否则返回自身）
    /// </summary>
    public IType resolved_type => inferred_type ?? this;

    public string name { get; }
    public IType? base_type => null;
    public IReadOnlyList<IType> type_arguments => [];
    public IReadOnlyList<ISymbol> members => [];

    public bool is_assignable_from(IType other)
    {
        if (is_resolved) return inferred_type!.is_assignable_from(other);

        return true;
    }

    public bool is_assignable_to(IType other)
    {
        if (is_resolved) return inferred_type!.is_assignable_to(other);

        return true;
    }

    public bool is_subtype_of(IType other)
    {
        if (is_resolved) return inferred_type!.is_subtype_of(other);

        return true;
    }

    public bool equals(IType? other)
    {
        return ReferenceEquals(this, other);
    }

    /// <summary>
    ///     绑定推导结果
    /// </summary>
    public void bind(IType type)
    {
        inferred_type = type;
    }

    public override string ToString()
    {
        return is_resolved ? $"'{name} = {inferred_type!.name}" : $"'{name}";
    }
}