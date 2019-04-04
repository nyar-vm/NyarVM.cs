using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     列裁剪：根据下游算子需求裁剪 Project 中未引用的列。
///     当嵌套 Project 的外层列是内层列的子集时，移除内层多余的列以减少中间结果宽度。
/// </summary>
public sealed class ProjectPruningRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "project-pruning";

    /// <inheritdoc />
    protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is Nodes.Project { columns: ["*"] }) continue;

            if (node is not Nodes.Project outer) continue;

            var innerClass = egraph.get_class(outer.data);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
            {
                if (innerNode is not Nodes.Project inner) continue;

                var prunedCols = outer.columns.Where(c => inner.columns.Contains(c) || c == "*").ToList();

                if (prunedCols.Count < outer.columns.Count && prunedCols.Count > 0)
                    yield return (node, new Nodes.Project(prunedCols, inner.data));
            }
        }
    }
}