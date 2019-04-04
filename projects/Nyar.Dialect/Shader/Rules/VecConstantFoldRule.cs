using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     向量常量折叠：当 Vec 的所有分量都是常量时，折叠为常量向量
/// </summary>
public sealed class VecConstantFoldRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "vec-constant-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Vec vec) continue;

            var constValues = new List<double>();
            var allConstant = true;

            foreach (var compId in vec.components)
            {
                var compClass = egraph.get_class(compId);
                var constNode = compClass?.nodes.OfType<Literal<long>>().FirstOrDefault();

                if (constNode is not null)
                {
                    constValues.Add(constNode.value);
                }
                else
                {
                    allConstant = false;
                    break;
                }
            }

            if (allConstant && constValues.Count > 0)
            {
                var packed = PackVecConstant(vec.size, constValues);
                yield return (node, new Literal<long>(packed));
            }
        }
    }

    private static long PackVecConstant(int size, List<double> values)
    {
        return size switch
        {
            2 => ((long)Math.Round(values[0]) & 0xFFFFFFFF) | ((long)Math.Round(values[1]) << 32),
            3 => ((long)Math.Round(values[0]) & 0xFFFF) | (((long)Math.Round(values[1]) & 0xFFFF) << 16) |
                 (((long)Math.Round(values[2]) & 0xFFFF) << 32),
            4 => ((long)Math.Round(values[0]) & 0xFF) | (((long)Math.Round(values[1]) & 0xFF) << 8) |
                 (((long)Math.Round(values[2]) & 0xFF) << 16) | (((long)Math.Round(values[3]) & 0xFF) << 24),
            _ => (long)Math.Round(values[0])
        };
    }
}