using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Standard.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Standard.Rules;

/// <summary>
///     切片零长度优化：SliceNew(ptr, 0) → 空切片，消除无效切片创建
/// </summary>
public sealed class SliceEmptyRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "slice-empty";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not SliceNew slice) continue;

            var lenClass = egraph.get_class(slice.length);
            if (lenClass is null) continue;

            foreach (var lenNode in lenClass.nodes)
                if (lenNode is Literal<long> { value: 0 })
                    yield return (node, new Literal<object?>(null));
        }
    }
}