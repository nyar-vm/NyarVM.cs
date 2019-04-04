using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     死着色器路径消除：当 ShaderStageDecl 的 Body 全部是 Nop 或 ComputeKernel 的 Body 是 Nop 时，
///     该着色器路径不可达，替换为 Nop
/// </summary>
public sealed class DeadShaderPathRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "dead-shader-path";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
            if (node is ShaderStageDecl stageDecl)
            {
                if (IsAllNopBody(egraph, stageDecl.body)) yield return (node, new Nop());
            }
            else if (node is ComputeKernel kernel)
            {
                if (IsNopNode(egraph, kernel.body)) yield return (node, new Nop());
            }
    }

    private static bool IsAllNopBody(EGraph<AlgebraNode> egraph, IReadOnlyList<Id> bodyIds)
    {
        if (bodyIds.Count == 0) return true;

        foreach (var bodyId in bodyIds)
            if (!IsNopNode(egraph, bodyId))
                return false;

        return true;
    }

    private static bool IsNopNode(EGraph<AlgebraNode> egraph, Id nodeId)
    {
        var nodeClass = egraph.get_class(nodeId);
        if (nodeClass is null) return false;

        foreach (var n in nodeClass.nodes)
            if (n is Nop)
                return true;

        return false;
    }
}