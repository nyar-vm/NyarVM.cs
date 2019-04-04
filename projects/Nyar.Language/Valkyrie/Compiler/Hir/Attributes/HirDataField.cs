using Nyar.Language.Valkyrie.Compiler.Hir._Def;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;

namespace Nyar.Language.Valkyrie.Compiler.Hir.Attributes;

/// <summary>
///     `[data]` 字段在 HIR 中的归一化描述。
/// </summary>
public sealed record HirDataField
{
    public string name { get; init; }
    public HirTypeRef type { get; init; }
    public string binding_name { get; init; }
    /// <summary>
    /// [alias]
    /// </summary>
    public IReadOnlyList<string> aliases { get; init; }
    /// <summary>
    /// [ignore]
    /// </summary>
    public bool ignore { get; init; }
    public bool flatten { get; init; }
    public int? order { get; init; }
    public bool skip_when_null { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public bool skip_when_default { get; init; }
    
    /// <summary>
    ///     `[data]` 字段在 HIR 中的归一化描述。
    /// </summary>
    public HirDataField(string name,
        HirTypeRef type,
        string binding_name,
        IReadOnlyList<string> aliases,
        bool ignore,
        bool flatten,
        int? order,
        bool skip_when_null,
        bool skip_when_default
    )
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
    ///     字段定义元数据视图。
    /// </summary>
    public HirFieldDef definition => new(
        name,
        type,
        binding_name,
        aliases,
        ignore,
        flatten,
        order,
        skip_when_null,
        skip_when_default
    );
    
    /// <summary>
    ///     判断字段是否可按指定名字参与反序列化绑定。
    /// </summary>
    public bool matches_deserializable_name(string fieldName)
    {
        return definition.matches_deserializable_name(fieldName);
    }

    /// <summary>
    ///     判断字段是否可按指定名字参与成员访问。
    /// </summary>
    public bool matches_member_name(string fieldName)
    {
        return definition.matches_member_name(fieldName);
    }
}