using Nyar.Dialect.Game.Nodes;
using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;
using GameParallel = Nyar.Dialect.Game.Nodes.Parallel;
using GameAction = Nyar.Dialect.Game.Nodes.Action;

namespace Nyar.Dialect.Game.Cost;

/// <summary>
///     Game 方言节点的成本估算钩子
/// </summary>
public sealed class GameCostHook : ICostModelHook
{
    /// <inheritdoc />
    public bool CanHandle(AlgebraNode node)
    {
        return node is Sequence or Selector or GameParallel or Decorator or Condition
            or GameAction or UtilityScore or UtilitySelect or RuleMatch or RuleSet
            or StateQuery or StateUpdate or EventTrigger or EventListen
            or PathFind or PerceptionQuery
            or SpawnEntity or DestroyEntity or AddComponent or GetComponent
            or SetComponent or RemoveComponent or HasComponent
            or DefineComponent or DefineSystem or QueryEntities or WorldUpdate;
    }

    /// <inheritdoc />
    public CostVector Estimate(AlgebraNode node)
    {
        return node switch
        {
            Sequence seq => new CostVector(seq.children.Count * 10, 0, 0, 0),
            Selector sel => new CostVector(sel.children.Count * 8, 0, 0, 0),
            GameParallel par => new CostVector(par.children.Count * 15, 0, 0, 0),
            Decorator dec => dec.type switch
            {
                DecoratorType.Invert => CostVector.from_latency(1),
                DecoratorType.Repeat => CostVector.from_latency(5),
                DecoratorType.UntilFail => CostVector.from_latency(5),
                DecoratorType.Cooldown => CostVector.from_latency(2),
                DecoratorType.Timeout => CostVector.from_latency(2),
                _ => CostVector.from_latency(2)
            },
            Condition => CostVector.from_latency(5),
            GameAction => CostVector.from_latency(20),
            UtilityScore => CostVector.from_latency(15),
            UtilitySelect sel => new CostVector(sel.options.Count * 12, 0, 0, 0),
            RuleMatch => CostVector.from_latency(10),
            RuleSet rs => new CostVector(rs.rules.Count * 10, 0, 0, 0),
            StateQuery => CostVector.from_latency(8),
            StateUpdate => CostVector.from_latency(12),
            EventTrigger => CostVector.from_latency(15),
            EventListen => CostVector.from_latency(10),
            PathFind => new CostVector(500, 0, 0, 0),
            PerceptionQuery => new CostVector(200, 0, 0, 0),
            SpawnEntity => CostVector.from_latency(30),
            DestroyEntity => CostVector.from_latency(20),
            AddComponent => CostVector.from_latency(25),
            GetComponent => CostVector.from_latency(10),
            SetComponent => CostVector.from_latency(15),
            RemoveComponent => CostVector.from_latency(20),
            HasComponent => CostVector.from_latency(5),
            DefineComponent def => new CostVector(10 + def.fields.Count * 2, 0, 0, 0),
            DefineSystem def => new CostVector(15 + def.before.Count + def.after.Count, 0, 0, 0),
            QueryEntities => new CostVector(100, 0, 0, 0),
            WorldUpdate => new CostVector(200, 0, 0, 0),
            _ => CostVector.zero
        };
    }
}