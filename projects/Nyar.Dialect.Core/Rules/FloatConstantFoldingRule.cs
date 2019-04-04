using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     浮点常量折叠：对 FloatConstant 操作数进行编译期求值
/// </summary>
public sealed class FloatConstantFoldingRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "float-constant-folding";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes.ToList())
        {
            var result = TryFold(egraph, node);
            if (result is not null) yield return (node, result);
        }
    }

    private static AlgebraNode? TryFold(EGraph<AlgebraNode> egraph, AlgebraNode node)
    {
        return node switch
        {
            Add add => TryFoldFloatBinary(egraph, add.left, add.right, (a, b) => a + b),
            Sub sub => TryFoldFloatBinary(egraph, sub.left, sub.right, (a, b) => a - b),
            Mul mul => TryFoldFloatBinary(egraph, mul.left, mul.right, (a, b) => a * b),
            Div div => TryFoldFloatBinaryDiv(egraph, div.left, div.right),
            Rem rem => TryFoldFloatBinaryRem(egraph, rem.left, rem.right),
            Neg neg => TryFoldFloatUnary(egraph, neg.operand, a => -a),
            Cmp cmp => TryFoldFloatCmp(egraph, cmp),
            _ => null
        };
    }

    private static AlgebraNode? TryFoldFloatBinary(EGraph<AlgebraNode> egraph, Id left, Id right, Func<double, double, double> op)
    {
        if (!TryGetFloatConstant(egraph, left, out var a)) return null;

        if (!TryGetFloatConstant(egraph, right, out var b)) return null;

        var result = op(a, b);

        if (double.IsNaN(result) || double.IsInfinity(result)) return null;

        return new Literal<double>(result);
    }

    private static AlgebraNode? TryFoldFloatBinaryDiv(EGraph<AlgebraNode> egraph, Id left, Id right)
    {
        if (!TryGetFloatConstant(egraph, left, out var a)) return null;

        if (!TryGetFloatConstant(egraph, right, out var b)) return null;

        if (b == 0.0) return null;

        var result = a / b;

        if (double.IsNaN(result) || double.IsInfinity(result)) return null;

        return new Literal<double>(result);
    }

    private static AlgebraNode? TryFoldFloatBinaryRem(EGraph<AlgebraNode> egraph, Id left, Id right)
    {
        if (!TryGetFloatConstant(egraph, left, out var a)) return null;

        if (!TryGetFloatConstant(egraph, right, out var b)) return null;

        if (b == 0.0) return null;

        var result = a % b;

        if (double.IsNaN(result) || double.IsInfinity(result)) return null;

        return new Literal<double>(result);
    }

    private static AlgebraNode? TryFoldFloatUnary(EGraph<AlgebraNode> egraph, Id operand, Func<double, double> op)
    {
        if (!TryGetFloatConstant(egraph, operand, out var a)) return null;

        var result = op(a);

        if (double.IsNaN(result) || double.IsInfinity(result)) return null;

        return new Literal<double>(result);
    }

    private static AlgebraNode? TryFoldFloatCmp(EGraph<AlgebraNode> egraph, Cmp cmp)
    {
        if (!TryGetFloatConstant(egraph, cmp.left, out var a)) return null;

        if (!TryGetFloatConstant(egraph, cmp.right, out var b)) return null;

        var result = cmp.op switch
        {
            CompareOp.eq => a == b,
            CompareOp.ne => a != b,
            CompareOp.lt => a < b,
            CompareOp.le => a <= b,
            CompareOp.gt => a > b,
            CompareOp.ge => a >= b,
            _ => (bool?)null
        };

        return result is not null ? new Literal<bool>(result.Value) : null;
    }

    private static bool TryGetFloatConstant(EGraph<AlgebraNode> egraph, Id id, out double value)
    {
        value = 0;
        var eclass = egraph.get_class(id);
        if (eclass is null) return false;

        foreach (var node in eclass.nodes)
            if (node is Literal<double> fc)
            {
                value = fc.value;
                return true;
            }

        return false;
    }
}