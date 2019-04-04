using Nyar.Dialect.Quantum.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Quantum.Rules;

/// <summary>
///     Hadamard 幂等律：H(H(q)) == q
/// </summary>
public sealed class HadamardIdempotentRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "hadamard-idempotent";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not SingleQubitGate { type: SingleGateType.H } outer) continue;

            var targetClass = egraph.get_class(outer.target);
            if (targetClass is null) continue;

            foreach (var targetNode in targetClass.nodes)
                if (targetNode is SingleQubitGate { type: SingleGateType.H } inner)
                    yield return (node, new SingleQubitGate(SingleGateType.H, inner.target));
        }
    }
}

/// <summary>
///     Pauli-X 幂等律：X(X(q)) == I (恒等)
/// </summary>
public sealed class PauliXIdempotentRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "pauli-x-idempotent";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not SingleQubitGate { type: SingleGateType.X } outer) continue;

            var targetClass = egraph.get_class(outer.target);
            if (targetClass is null) continue;

            foreach (var targetNode in targetClass.nodes)
                if (targetNode is SingleQubitGate { type: SingleGateType.X } inner)
                    // X(X(q)) = q，即恒等操作
                    yield return (node, new SingleQubitGate(SingleGateType.H, inner.target));
        }
    }
}

/// <summary>
///     CNOT 消除：CX(q, q) == I (控制位和目标位相同则为恒等)
/// </summary>
public sealed class CnotSelfEliminationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "cnot-self-elimination";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not ControlledGate { type: ControlledGateType.CX } cx) continue;

            // 如果控制位和目标位在等价类中相同，则消除
            if (cx.control.Equals(cx.target)) yield return (node, new SingleQubitGate(SingleGateType.H, cx.target));
        }
    }
}

/// <summary>
///     电路扁平化：QuantumCircuit([QuantumCircuit(a, b), c]) == QuantumCircuit(a, b, c)
/// </summary>
public sealed class CircuitFlattenRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "circuit-flatten";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not QuantumCircuit outer) continue;

            var flattened = new List<Id>();
            var changed = false;

            foreach (var op in outer.operations)
            {
                var opClass = egraph.get_class(op);
                if (opClass is null)
                {
                    flattened.Add(op);
                    continue;
                }

                var foundInner = false;
                foreach (var opNode in opClass.nodes)
                    if (opNode is QuantumCircuit inner)
                    {
                        flattened.AddRange(inner.operations);
                        foundInner = true;
                        changed = true;
                        break;
                    }

                if (!foundInner) flattened.Add(op);
            }

            if (changed) yield return (node, new QuantumCircuit(flattened));
        }
    }
}