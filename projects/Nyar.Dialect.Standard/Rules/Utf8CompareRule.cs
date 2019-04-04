using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Standard.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Standard.Rules;

/// <summary>
///     UTF-8 文本比较常量折叠：Utf8Compare(Utf8Constant("abc"), Utf8Constant("abc")) → Constant(0)
/// </summary>
public sealed class Utf8CompareRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "utf8-compare-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Utf8Compare cmp) continue;

            if (!egraph.TryGetConstant<string>(cmp.left, out var left)) continue;

            if (!egraph.TryGetConstant<string>(cmp.right, out var right)) continue;

            var result = string.CompareOrdinal(left, right);
            yield return (node, new Literal<long>(result));
        }
    }
}
