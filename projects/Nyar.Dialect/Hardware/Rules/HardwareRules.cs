using Nyar.Dialect.Hardware.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Hardware.Rules;

/// <summary>
///     逻辑化简规则：And(a, Or(b, Not(b))) → a（吸收律）
/// </summary>
public sealed class LogicSimplificationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "logic-simplification";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Gate { type: GateType.And } andGate) continue;
            if (andGate.inputs.Count != 2) continue;

            var rightClass = egraph.get_class(andGate.inputs[1]);
            if (rightClass is null) continue;

            foreach (var rightNode in rightClass.nodes)
            {
                if (rightNode is not Gate { type: GateType.Or } orGate) continue;
                if (orGate.inputs.Count != 2) continue;

                var innerClass = egraph.get_class(orGate.inputs[1]);
                if (innerClass is null) continue;

                foreach (var innerNode in innerClass.nodes)
                {
                    if (innerNode is not Gate { type: GateType.Not } notGate) continue;
                    if (notGate.inputs.Count != 1) continue;

                    if (egraph.union_find.find(orGate.inputs[0]) == egraph.union_find.find(notGate.inputs[0]))
                        yield return (node, new Gate(GateType.And, [andGate.inputs[0]]));
                }
            }
        }
    }
}

/// <summary>
///     寄存器重定时规则（占位，待实现）
/// </summary>
public sealed class RegisterRetimingRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "register-retiming";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        yield break;
    }
}