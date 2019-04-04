using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Web.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Web.Rules;

/// <summary>
///     静态属性合并：合并相同元素的多个 Attr
/// </summary>
public sealed class StaticAttrMergeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "static-attr-merge";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Element { attributes.Count: >= 2 } element) continue;

            var mergedAttrs = new List<Id>();
            var seenNames = new HashSet<string>();
            var hasDuplicates = false;

            foreach (var attrId in element.attributes)
            {
                var attrName = TryGetAttrName(egraph, attrId);
                if (attrName is not null)
                    if (!seenNames.Add(attrName))
                    {
                        hasDuplicates = true;
                        continue;
                    }

                mergedAttrs.Add(attrId);
            }

            if (!hasDuplicates) continue;

            yield return (node, new Element(element.tag, [.. mergedAttrs], element.children));
        }
    }

    private static string? TryGetAttrName(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) return null;

        foreach (var node in eclass.nodes)
            if (node is Attr attr)
                return attr.name;

        return null;
    }
}

/// <summary>
///     条件折叠：Cond(true, then, else) -> then
/// </summary>
public sealed class CondFoldRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "cond-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Cond cond) continue;

            var conditionValue = TryGetBooleanConstant(egraph, cond.condition);
            if (conditionValue is null) continue;

            var branchId = conditionValue.Value ? cond.then_branch : cond.else_branch;
            var branchClass = egraph.get_class(branchId);
            if (branchClass is null) continue;

            foreach (var branchNode in branchClass.nodes)
            {
                yield return (node, branchNode);
                yield break;
            }
        }
    }

    private static bool? TryGetBooleanConstant(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) return null;

        foreach (var node in eclass.nodes)
            if (node is Literal<bool> bc)
                return bc.value;

        return null;
    }
}