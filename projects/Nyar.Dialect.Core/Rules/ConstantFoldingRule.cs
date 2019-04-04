using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     整数常量折叠规则（OA 节点版），输出 <c>Literal&lt;long&gt;</c> 和 <c>Literal&lt;bool&gt;</c>
/// </summary>
public sealed class ConstantFoldingRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "constant-folding";

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
            Add add => TryFoldBinary(egraph, add.left, add.right, (a, b) => a + b),
            Sub sub => TryFoldBinary(egraph, sub.left, sub.right, (a, b) => a - b),
            Mul mul => TryFoldBinary(egraph, mul.left, mul.right, (a, b) => a * b),
            Div div => TryFoldBinaryDiv(egraph, div.left, div.right),
            Rem rem => TryFoldBinaryRem(egraph, rem.left, rem.right),
            Neg neg => TryFoldUnary(egraph, neg.operand, a => -a),
            Not not => TryFoldBoolUnary(egraph, not.operand),
            Cmp cmp => TryFoldCmp(egraph, cmp),
            _ => null
        };
    }

    private static AlgebraNode? TryFoldBinary(EGraph<AlgebraNode> egraph, Id left, Id right, Func<long, long, long> op)
    {
        if (!TryGetConstant(egraph, left, out var a)) return null;

        if (!TryGetConstant(egraph, right, out var b)) return null;

        try
        {
            return new Literal<long>(checked(op(a, b)));
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static AlgebraNode? TryFoldBinaryDiv(EGraph<AlgebraNode> egraph, Id left, Id right)
    {
        if (!TryGetConstant(egraph, left, out var a)) return null;

        if (!TryGetConstant(egraph, right, out var b)) return null;

        if (b == 0) return null;

        return new Literal<long>(a / b);
    }

    private static AlgebraNode? TryFoldBinaryRem(EGraph<AlgebraNode> egraph, Id left, Id right)
    {
        if (!TryGetConstant(egraph, left, out var a)) return null;

        if (!TryGetConstant(egraph, right, out var b)) return null;

        if (b == 0) return null;

        return new Literal<long>(a % b);
    }

    private static AlgebraNode? TryFoldUnary(EGraph<AlgebraNode> egraph, Id operand, Func<long, long> op)
    {
        if (!TryGetConstant(egraph, operand, out var a)) return null;

        try
        {
            return new Literal<long>(checked(op(a)));
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static AlgebraNode? TryFoldBoolUnary(EGraph<AlgebraNode> egraph, Id operand)
    {
        if (!TryGetBoolConstant(egraph, operand, out var a)) return null;

        return new Literal<bool>(!a);
    }

    private static AlgebraNode? TryFoldCmp(EGraph<AlgebraNode> egraph, Cmp cmp)
    {
        if (!TryGetConstant(egraph, cmp.left, out var a)) return null;

        if (!TryGetConstant(egraph, cmp.right, out var b)) return null;

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

    /// <summary>
    ///     从等价类中提取整数常量
    /// </summary>
    private static bool TryGetConstant(EGraph<AlgebraNode> egraph, Id id, out long value)
    {
        value = 0;
        var targetClass = egraph.get_class(id);
        if (targetClass is null) return false;

        foreach (var node in targetClass.nodes)
            if (node is Literal<long> lit)
            {
                value = lit.value;
                return true;
            }

        return false;
    }

    /// <summary>
    ///     从等价类中提取布尔常量
    /// </summary>
    private static bool TryGetBoolConstant(EGraph<AlgebraNode> egraph, Id id, out bool value)
    {
        value = false;
        var targetClass = egraph.get_class(id);
        if (targetClass is null) return false;

        foreach (var node in targetClass.nodes)
            if (node is Literal<bool> lit)
            {
                value = lit.value;
                return true;
            }

        return false;
    }
}