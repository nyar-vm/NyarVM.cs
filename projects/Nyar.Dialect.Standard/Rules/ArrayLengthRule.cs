using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Standard.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Standard.Rules;

/// <summary>
///     数组长度规则：常量数组长度折叠
/// </summary>
public sealed class ArrayLengthRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "array-length";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not ArrayLength length) continue;

            if (!TryGetArrayNew(egraph, length.array, out var arrayNew)) continue;

            if (!egraph.TryGetConstant<long>(arrayNew.length, out var constantValue)) continue;

            yield return (node, new Literal<long>(constantValue));
        }
    }

    private static bool TryGetArrayNew(EGraph<AlgebraNode> egraph, Id id, out ArrayNew arrayNew)
    {
        arrayNew = null!;
        var eclass = egraph.get_class(id);
        if (eclass is null) return false;

        foreach (var node in eclass.nodes)
            if (node is ArrayNew an)
            {
                arrayNew = an;
                return true;
            }

        return false;
    }
}