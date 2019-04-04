using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     Join 重排序：基于行数统计将小表放在构建侧（左侧），
///     使 Hash Join 的构建阶段处理更少的数据，降低内存占用和探测代价。
///     仅对 Inner Join 生效（外连接不能随意交换顺序）。
/// </summary>
public sealed class JoinReorderRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "join-reorder";

    /// <inheritdoc />
    protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Join { type: JoinType.inner } join) continue;

            var leftSize = EstimateSubtreeSize(egraph, join.left);
            var rightSize = EstimateSubtreeSize(egraph, join.right);

            if (rightSize < leftSize)
                yield return (node, new Join(JoinType.inner, join.right, join.left, join.condition));
        }
    }

    /// <summary>
    ///     估计子树的输出行数，优先查找 TableStats 节点获取真实统计，
    ///     回退到基于操作类型的启发式估算
    /// </summary>
    private static long EstimateSubtreeSize(EGraph<Oa> egraph, Id nodeId)
    {
        return EstimateSubtreeSizeCore(egraph, nodeId, 0);
    }

    /// <summary>
    ///     递归估算子树行数，深度超过阈值时返回默认值以防止无限递归
    /// </summary>
    private static long EstimateSubtreeSizeCore(EGraph<Oa> egraph, Id nodeId, int depth)
    {
        if (depth > 5) return 1000;

        var eclass = egraph.get_class(nodeId);
        if (eclass is null) return 1000;

        foreach (var node in eclass.nodes)
            switch (node)
            {
                case Scan scan:
                {
                    var stats = FindTableStats(egraph, scan.table_name);
                    return stats?.row_count ?? 1000;
                }
                case IndexScan indexScan:
                {
                    var stats = FindTableStats(egraph, indexScan.table_name);
                    return stats is not null
                        ? (long)(stats.row_count * 0.1)
                        : 100;
                }
                case Filter filter:
                    return (long)(EstimateSubtreeSizeCore(egraph, filter.data, depth + 1) * 0.3);
                case Nodes.Project project:
                    return EstimateSubtreeSizeCore(egraph, project.data, depth + 1);
                case Join join:
                {
                    var leftSize = EstimateSubtreeSizeCore(egraph, join.left, depth + 1);
                    var rightSize = EstimateSubtreeSizeCore(egraph, join.right, depth + 1);
                    return Math.Min(leftSize, rightSize);
                }
                case GroupBy:
                    return 100;
                case Having having:
                    return EstimateSubtreeSizeCore(egraph, having.data, depth + 1);
                case OrderBy orderBy:
                    return EstimateSubtreeSizeCore(egraph, orderBy.data, depth + 1);
                case Limit:
                    return 10;
                case Distinct distinct:
                    return (long)(EstimateSubtreeSizeCore(egraph, distinct.data, depth + 1) * 0.5);
                case WindowFunction wf:
                    return EstimateSubtreeSizeCore(egraph, wf.data, depth + 1);
                case Union union:
                {
                    var leftSize = EstimateSubtreeSizeCore(egraph, union.left, depth + 1);
                    var rightSize = EstimateSubtreeSizeCore(egraph, union.right, depth + 1);
                    return leftSize + rightSize;
                }
                case Subquery:
                    return 100;
            }

        return 1000;
    }

    /// <summary>
    ///     在 EGraph 中查找指定表名的 TableStats 节点，
    ///     用于获取真实的行数统计信息
    /// </summary>
    private static TableStats? FindTableStats(EGraph<Oa> egraph, string tableName)
    {
        foreach (var eclass in egraph.classes.Values)
        foreach (var node in eclass.nodes)
            if (node is TableStats stats && stats.table_name == tableName)
                return stats;

        return null;
    }
}