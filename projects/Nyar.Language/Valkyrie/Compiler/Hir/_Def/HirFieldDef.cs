using Nyar.Language.Valkyrie.Compiler.Hir._Ref;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Def;

/// <summary>
///     HIR 字段定义元数据。
/// </summary>
public sealed record HirFieldDef
{
    public string name { get; init; }
    public HirTypeRef type { get; init; }
    public string binding_name { get; init; }
    public IReadOnlyList<string> aliases { get; init; }
    public bool ignore { get; init; }
    public bool flatten { get; init; }
    public int? order { get; init; }
    public bool skip_when_null { get; init; }
    public bool skip_when_default { get; init; }
    /// <summary>
    ///     HIR 字段定义元数据。
    /// </summary>
    public HirFieldDef(string name,
        HirTypeRef type,
        string binding_name,
        IReadOnlyList<string> aliases,
        bool ignore,
        bool flatten,
        int? order,
        bool skip_when_null,
        bool skip_when_default)
    {
        this.name = name;
        this.type = type;
        this.binding_name = binding_name;
        this.aliases = aliases;
        this.ignore = ignore;
        this.flatten = flatten;
        this.order = order;
        this.skip_when_null = skip_when_null;
        this.skip_when_default = skip_when_default;
    }

    /// <summary>
    ///     判断字段声明名是否匹配。
    /// </summary>
    public bool matches_declared_name(string fieldName)
    {
        return string.Equals(name, fieldName, StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断字段绑定名是否匹配。
    /// </summary>
    public bool matches_binding_name(string fieldName)
    {
        return string.Equals(binding_name, fieldName, StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断字段别名是否匹配。
    /// </summary>
    public bool matches_alias(string fieldName)
    {
        return aliases.Any(alias => string.Equals(alias, fieldName, StringComparison.Ordinal));
    }

    /// <summary>
    ///     判断字段是否可以按给定名字被成员访问命中。
    /// </summary>
    public bool matches_member_name(string fieldName)
    {
        return matches_declared_name(fieldName) ||
               matches_binding_name(fieldName) ||
               matches_alias(fieldName);
    }

    /// <summary>
    ///     判断字段是否可以按给定名字参与反序列化绑定。
    /// </summary>
    public bool matches_deserializable_name(string fieldName)
    {
        return matches_binding_name(fieldName) || matches_alias(fieldName);
    }
}
