using Nyar.Dialect.Agent.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Agent.Rules;

/// <summary>
///     智能体集合扁平化：AgentSet([AgentSet(a, b), c]) == AgentSet(a, b, c)
/// </summary>
public sealed class AgentSetFlattenRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "agentset-flatten";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not AgentSet outer) continue;

            var flattened = new List<Id>();
            var changed = false;

            foreach (var agent in outer.agents)
            {
                var agentClass = egraph.get_class(agent);
                if (agentClass is null)
                {
                    flattened.Add(agent);
                    continue;
                }

                var foundInner = false;
                foreach (var agentNode in agentClass.nodes)
                    if (agentNode is AgentSet inner)
                    {
                        flattened.AddRange(inner.agents);
                        foundInner = true;
                        changed = true;
                        break;
                    }

                if (!foundInner) flattened.Add(agent);
            }

            if (changed) yield return (node, new AgentSet(flattened, outer.relation));
        }
    }
}

/// <summary>
///     计划动作序列扁平化：Plan([Plan(a, b), c]) == Plan(a, b, c)
/// </summary>
public sealed class PlanFlattenRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "plan-flatten";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Plan outer) continue;

            var flattenedActions = new List<Id>();
            var changed = false;

            foreach (var action in outer.actions)
            {
                var actionClass = egraph.get_class(action);
                if (actionClass is null)
                {
                    flattenedActions.Add(action);
                    continue;
                }

                var foundInner = false;
                foreach (var actionNode in actionClass.nodes)
                    if (actionNode is Plan inner)
                    {
                        flattenedActions.AddRange(inner.actions);
                        foundInner = true;
                        changed = true;
                        break;
                    }

                if (!foundInner) flattenedActions.Add(action);
            }

            if (changed) yield return (node, new Plan(flattenedActions, outer.orderingConstraints));
        }
    }
}

/// <summary>
///     HTN 任务分解：将 HTN 任务替换为其方法中的子任务
/// </summary>
public sealed class HtnDecompositionRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "htn-decomposition";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not HtnTask htnTask) continue;

            foreach (var method in htnTask.methods)
            {
                var methodClass = egraph.get_class(method);
                if (methodClass is null) continue;

                foreach (var methodNode in methodClass.nodes)
                    if (methodNode is HtnMethod htnMethod)
                        yield return (node, new Plan(htnMethod.subtasks, new List<Id>()));
            }
        }
    }
}

/// <summary>
///     信念状态合并：合并两个信念状态中的事实
/// </summary>
public sealed class BeliefStateMergeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "belief-state-merge";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not BeliefUpdate update) continue;

            var currentClass = egraph.get_class(update.currentBeliefs);
            if (currentClass is null) continue;

            foreach (var currentNode in currentClass.nodes)
            {
                if (currentNode is not BeliefState current) continue;

                var mergedFacts = new Dictionary<string, Id>(current.facts);
                // 观察可以添加新事实或更新现有事实
                yield return (node, new BeliefState(mergedFacts));
            }
        }
    }
}