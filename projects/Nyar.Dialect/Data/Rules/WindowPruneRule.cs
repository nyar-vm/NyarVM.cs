using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     窗口函数剪枝：当窗口函数的 PartitionBy + OrderBy 未被后续节点使用时，可消除
///     以及 Distinct 恒等消除：Distinct([], data) → data（无列指定等于不去重）
/// </summary>
public sealed class WindowPruneRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "window-prune";

    /// <inheritdoc />
    protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
            if (node is Distinct { columns.Count: 0 } distinct)
                yield return (node, new Sym("__data_passthrough__"));
    }
}