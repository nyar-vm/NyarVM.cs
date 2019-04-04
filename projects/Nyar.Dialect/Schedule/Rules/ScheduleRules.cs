using Nyar.Dialect.Schedule.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Schedule.Rules;

/// <summary>
///     任务集合扁平化：TaskSet(TaskSet(x)) == TaskSet(x)
/// </summary>
public sealed class TaskSetFlattenRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "taskset-flatten";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not TaskSet outer) continue;

            var taskClass = egraph.get_class(outer.tasks);
            if (taskClass is null) continue;

            foreach (var taskNode in taskClass.nodes)
                if (taskNode is TaskSet inner)
                {
                    yield return (node, new TaskSet(inner.tasks));
                    break;
                }
        }
    }
}

/// <summary>
///     前置依赖传递性：A -> B 且 B -> C 蕴含 A -> C
/// </summary>
public sealed class PrecedenceTransitivityRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "precedence-transitivity";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Precedence { beforeTask: var a, afterTask: var b }) continue;

            var bClass = egraph.get_class(b);
            if (bClass is null) continue;

            foreach (var bNode in bClass.nodes)
            {
                if (bNode is not Precedence { beforeTask: var b2, afterTask: var c }) continue;

                if (b.Equals(b2)) yield return (node, new Precedence(a, c));
            }
        }
    }
}

/// <summary>
///     互斥对称性：MutualExclusion(A, B) == MutualExclusion(B, A)
/// </summary>
public sealed class MutualExclusionCommutativeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "mutual-exclusion-commutative";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not MutualExclusion me) continue;

            yield return (node, new MutualExclusion(me.taskB, me.taskA));
        }
    }
}

/// <summary>
///     多目标权重归一化：MultiObjective(MultiObjective(a, w1), w2) 可与其他权重组合
/// </summary>
public sealed class MultiObjectiveMergeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "multi-objective-merge";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not MultiObjective outer) continue;

            var objClass = egraph.get_class(outer.objectives);
            if (objClass is null) continue;

            foreach (var objNode in objClass.nodes)
                if (objNode is MultiObjective inner)
                {
                    var mergedWeights = new List<float>();
                    for (var j = 0; j < inner.weights.Count; j++)
                        mergedWeights.Add(outer.weights[0] * inner.weights[j]);

                    yield return (node,
                        new MultiObjective(inner.objectives, mergedWeights));
                    break;
                }
        }
    }
}