using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     投影合并：Project(cols1, Project(cols2, data)) → Project(cols1, data)
///     内层投影被外层投影覆盖，可直接消除
/// </summary>
public sealed class ProjectMergeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "project-merge";

    /// <inheritdoc />
    protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Nodes.Project outer) continue;

            var innerClass = egraph.get_class(outer.data);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
                if (innerNode is Nodes.Project inner)
                    yield return (node, new Nodes.Project(outer.columns, inner.data));
        }
    }
}