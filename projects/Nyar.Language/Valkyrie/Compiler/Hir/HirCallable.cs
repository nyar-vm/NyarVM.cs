using Nyar.Analyzer.Semantic;
using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler.Hir._Def;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR 可调用项�?
/// </summary>
public abstract class HirCallable
{
    protected HirCallable(
        string name,
        IReadOnlyList<HirSymbolRef> parameters,
        HirTypeRef returnType,
        HirCallableSemantics semantics,
        IReadOnlyList<HirAttribute> surfaceAttributes,
        string? namespaceName,
        EffectSet effectSet)
    {
        this.name = name;
        this.parameters = parameters;
        return_type = returnType;
        this.semantics = semantics;
        surface_attributes = surfaceAttributes;
        namespace_name = namespaceName;
        effect_set = effectSet;
    }

    /// <summary>
    ///     供后续降级使用的唯一名称�?
    /// </summary>
    public string name { get; }

    public IReadOnlyList<HirSymbolRef> parameters { get; }

    public HirTypeRef return_type { get; }

    public HirCallableSemantics semantics { get; }

    public bool is_logical_entry => semantics.is_logical_entry;

    /// <summary>
    ///     未被元语言消费的表面属性列表（�?[clr("System.Console", "System.Console", "Write")]）�?
    /// </summary>
    public IReadOnlyList<HirAttribute> surface_attributes { get; }

    /// <summary>
    ///     所属命名空间�?
    /// </summary>
    public string? namespace_name { get; }

    /// <summary>
    ///     所属命名空间的结构化表示�?
    /// </summary>
    public SemanticNameSpace namespace_path => ValkyrieNameSpace.parse(namespace_name);

    /// <summary>
    ///     效应签名，表示该可调用项可能抛出的效应集合�?
    ///     纯函数为 <see cref="EffectSet.pure" />�?
    /// </summary>
    public EffectSet effect_set { get; }

    /// <summary>
    ///     可调用项对应的语法节点�?
    /// </summary>
    public abstract AstNode syntax { get; }

    /// <summary>
    ///     成员名称（方法名），函数场景下从 <see cref="name" /> 解析得到�?
    /// </summary>
    public abstract string member_name { get; }

    /// <summary>
    ///     所属类型引用；函数场景下为占位类型�?
    /// </summary>
    public abstract HirTypeRef owner_type { get; }

    /// <summary>
    ///     合同类型引用；仅�?imply 实现�?trait 方法有效，否则为 <c>null</c>�?
    /// </summary>
    public abstract HirTypeRef? contract_type { get; }

    /// <summary>
    ///     方法来源种类（inherent/trait/imply）�?
    /// </summary>
    public abstract HirMethodKind kind { get; }

    /// <summary>
    ///     Trait 方法槽位；仅�?trait 方法和其对应�?imply 实现有效�?
    /// </summary>
    public abstract int? slot_index { get; }

    /// <summary>
    ///     可调用项函数体�?
    /// </summary>
    public abstract BlockStmt? body { get; }

    /// <summary>
    ///     判断方法成员名是否匹配�?
    /// </summary>
    public bool matches_member_name(string expectedMemberName)
    {
        return string.Equals(member_name, expectedMemberName, StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断实例调用时的方法参数个数是否匹配�?
    /// </summary>
    public bool matches_instance_arity(int argumentCount)
    {
        return parameters.Count == argumentCount + 1;
    }

    /// <summary>
    ///     判断显式静态调用时的方法参数个数是否匹配�?
    /// </summary>
    public bool matches_explicit_arity(int argumentCount)
    {
        return parameters.Count == argumentCount;
    }

    /// <summary>
    ///     判断当前方法与目标方法签名是否语义一致�?
    /// </summary>
    public bool semantically_matches_signature(HirMethod other, bool requireSameMemberName = false)
    {
        if (requireSameMemberName && !matches_member_name(other.member_name))
        {
            return false;
        }

        if (!return_type.semantically_equals(other.return_type) ||
            parameters.Count != other.parameters.Count)
        {
            return false;
        }

        for (var index = 0; index < parameters.Count; index++)
        {
            if (!parameters[index].type.semantically_equals(other.parameters[index].type))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     将当前自由函数包装为一次静态调用解析结果�?
    /// </summary>
    public HirCallResolution to_call_resolution()
    {
        return new HirCallResolution(HirDispatchKind.@static, name, false, null, null, null);
    }

    /// <summary>
    ///     将当前方法包装为一次调用解析结果�?
    /// </summary>
    public HirCallResolution to_call_resolution(
        HirDispatchKind dispatch,
        bool injectReceiver,
        SemanticNamePath? witnessTraitName = null,
        string? resolvedMemberName = null)
    {
        return new HirCallResolution(
            dispatch,
            name,
            injectReceiver,
            slot_index,
            witnessTraitName,
            resolvedMemberName ?? member_name);
    }
}
