using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Physical;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     增强版谓词下推：将 Filter 中的谓词按引用列推到 Join 的左侧或右侧。
///     支持拆分 AND 谓词为独立合取项，分别下推到对应侧。
/// </summary>
public sealed class EnhancedPredicatePushdownRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "enhanced-predicate-pushdown";

    /// <inheritdoc />
    protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Filter filter) continue;

            var dataClass = egraph.get_class(filter.data);
            if (dataClass is null) continue;

            foreach (var dataNode in dataClass.nodes)
            {
                if (dataNode is not Join join) continue;

                var conjuncts = SplitConjuncts(egraph, filter.predicate);

                var leftPredicates = new List<Id>();
                var rightPredicates = new List<Id>();
                var remainingPredicates = new List<Id>();

                foreach (var conj in conjuncts)
                {
                    var refs = CollectColumnReferences(egraph, conj);
                    var leftRefs = GetColumnsFromSubtree(egraph, join.left);
                    var rightRefs = GetColumnsFromSubtree(egraph, join.right);

                    var referencesLeft = refs.Any(r => leftRefs.Contains(r));
                    var referencesRight = refs.Any(r => rightRefs.Contains(r));

                    if (referencesLeft && !referencesRight)
                        leftPredicates.Add(conj);
                    else if (referencesRight && !referencesLeft)
                        rightPredicates.Add(conj);
                    else
                        remainingPredicates.Add(conj);
                }

                var newLeft = join.left;
                var newRight = join.right;

                if (leftPredicates.Count > 0)
                {
                    var leftPred = CombinePredicates(egraph, leftPredicates);
                    newLeft = egraph.add(new Filter(leftPred, join.left));
                }

                if (rightPredicates.Count > 0)
                {
                    var rightPred = CombinePredicates(egraph, rightPredicates);
                    newRight = egraph.add(new Filter(rightPred, join.right));
                }

                var newJoin = new Join(join.type, newLeft, newRight, join.condition);

                if (remainingPredicates.Count > 0)
                {
                    var remainingPred = CombinePredicates(egraph, remainingPredicates);
                    yield return (node, new Filter(remainingPred, egraph.add(newJoin)));
                }
                else
                {
                    yield return (node, newJoin);
                }
            }
        }
    }

    /// <summary>
    ///     拆分 AND 谓词为独立合取项
    /// </summary>
    private static List<Id> SplitConjuncts(EGraph<Oa> egraph, Id predId)
    {
        var result = new List<Id>();
        var eclass = egraph.get_class(predId);
        if (eclass is null)
        {
            result.Add(predId);
            return result;
        }

        foreach (var node in eclass.nodes)
            if (node is And { left: var l, right: var r })
            {
                result.AddRange(SplitConjuncts(egraph, l));
                result.AddRange(SplitConjuncts(egraph, r));
                return result;
            }

        result.Add(predId);
        return result;
    }

    /// <summary>
    ///     使用 AND 合并多个谓词
    /// </summary>
    private static Id CombinePredicates(EGraph<Oa> egraph, List<Id> predicates)
    {
        if (predicates.Count == 0) return egraph.add(new Literal<bool>(true));

        if (predicates.Count == 1) return predicates[0];

        var result = predicates[0];
        for (var i = 1; i < predicates.Count; i++) result = egraph.add(new And(result, predicates[i]));

        return result;
    }

    /// <summary>
    ///     从谓词子树中收集列引用
    /// </summary>
    private static HashSet<string> CollectColumnReferences(EGraph<Oa> egraph, Id predId, int depth = 0)
    {
        var refs = new HashSet<string>();
        if (depth > 5) return refs;

        var eclass = egraph.get_class(predId);
        if (eclass is null) return refs;

        foreach (var node in eclass.nodes)
            if (node is Sym symbol)
            {
                refs.Add(symbol.name);
            }
            else if (node is And { left: var left, right: var right })
            {
                refs.UnionWith(CollectColumnReferences(egraph, left, depth + 1));
                refs.UnionWith(CollectColumnReferences(egraph, right, depth + 1));
            }
            else if (node is Not { operand: var coreNotOperand })
            {
                refs.UnionWith(CollectColumnReferences(egraph, coreNotOperand, depth + 1));
            }

        return refs;
    }

    /// <summary>
    ///     从子树中获取可用的列名
    /// </summary>
    private static HashSet<string> GetColumnsFromSubtree(EGraph<Oa> egraph, Id nodeId, int depth = 0)
    {
        var cols = new HashSet<string>();
        if (depth > 5) return cols;

        var eclass = egraph.get_class(nodeId);
        if (eclass is null) return cols;

        foreach (var node in eclass.nodes)
            if (node is Scan scan)
            {
                cols.Add(scan.table_name);
            }
            else if (node is IndexScan idxScan)
            {
                cols.Add(idxScan.table_name);
            }
            else if (node is PhysicalNode.Scan ikunScan)
            {
                cols.Add(ikunScan.table_name);
            }
            else if (node is Filter filter)
            {
                cols.UnionWith(GetColumnsFromSubtree(egraph, filter.data, depth + 1));
            }
            else if (node is Nodes.Project project)
            {
                cols.UnionWith(project.columns);
                cols.UnionWith(GetColumnsFromSubtree(egraph, project.data, depth + 1));
            }
            else if (node is Join join)
            {
                cols.UnionWith(GetColumnsFromSubtree(egraph, join.left, depth + 1));
                cols.UnionWith(GetColumnsFromSubtree(egraph, join.right, depth + 1));
            }

        return cols;
    }
}