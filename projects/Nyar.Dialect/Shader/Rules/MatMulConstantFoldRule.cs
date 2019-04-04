using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     矩阵乘法常量折叠：当 MatMul 的两侧都是常量矩阵时，编译期计算结果
/// </summary>
public sealed class MatMulConstantFoldRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "matmul-constant-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not MatMul matMul) continue;

            var leftConst = TryGetMatrixConstants(egraph, matMul.left);
            var rightConst = TryGetMatrixConstants(egraph, matMul.right);

            if (leftConst is null || rightConst is null) continue;

            var result = MultiplyMatrices(leftConst, rightConst);
            var elementIds = result.Select(v => FindOrCreateConstant(egraph, (long)Math.Round(v))).ToList();

            yield return (node, new Mat(4, 4, elementIds));
        }
    }

    private static List<double>? TryGetMatrixConstants(EGraph<AlgebraNode> egraph, Id matId)
    {
        var matClass = egraph.get_class(matId);
        var matNode = matClass?.nodes.OfType<Mat>().FirstOrDefault();
        if (matNode is null) return null;

        var values = new List<double>();

        foreach (var elemId in matNode.elements)
        {
            var elemClass = egraph.get_class(elemId);
            var constNode = elemClass?.nodes.OfType<Literal<long>>().FirstOrDefault();
            if (constNode is null) return null;

            values.Add(constNode.value);
        }

        return values;
    }

    private static List<double> MultiplyMatrices(List<double> left, List<double> right)
    {
        var n = (int)Math.Sqrt(left.Count);
        var result = new List<double>(n * n);

        for (var i = 0; i < n; i++)
        for (var j = 0; j < n; j++)
        {
            var sum = 0.0;
            for (var k = 0; k < n; k++) sum += left[i * n + k] * right[k * n + j];

            result.Add(sum);
        }

        return result;
    }

    private static Id FindOrCreateConstant(EGraph<AlgebraNode> egraph, long value)
    {
        foreach (var (_, eclass) in egraph.classes)
            if (eclass.nodes.OfType<Literal<long>>().Any(c => c.value == value))
                return eclass.id;

        var constNode = new Literal<long>(value);
        var newId = egraph.add(constNode);
        return newId;
    }
}