using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Standard.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Standard.Rules;

/// <summary>
///     UTF-8 文本长度常量折叠：
///     Utf8Len(Utf8Constant("hello")) → Constant(5)
///     Utf8Len(Utf8Constant("world")) → Constant(5)
/// </summary>
public sealed class Utf8LenRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "utf8-length-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Utf8Len length) continue;

            if (!egraph.TryGetConstant<string>(length.value, out var strValue)) continue;
            yield return (node, new Literal<long>(strValue.Length));
        }
    }
}
