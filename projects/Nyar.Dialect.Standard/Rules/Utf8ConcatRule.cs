using Nyar.Dialect.Standard.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.IR.Strategy;

namespace Nyar.Dialect.Standard.Rules;

/// <summary>
///     UTF-8 文本拼接规则：空文本拼接消除
/// </summary>
public sealed class Utf8ConcatRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "utf8-concat";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Utf8Concat concat) continue;

            if (egraph.TryGetConstant<string>(concat.left, out var leftValue) && leftValue == "")
                yield return (node, new StrategyNode.Extension("identity", [concat.right]));

            if (egraph.TryGetConstant<string>(concat.right, out var rightValue) && rightValue == "")
                yield return (node, new StrategyNode.Extension("identity", [concat.left]));
        }
    }
}
