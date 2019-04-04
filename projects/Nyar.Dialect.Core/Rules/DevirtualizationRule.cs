using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Physical;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core;

/// <summary>
///     调用去虚拟化规则：将高级分派降级为低开销分派
///     Dynamic → Witness：当 Witness 引用可用时（IC 反馈已建立见证表映射）
///     Dynamic/Witness → Static：当 MethodIndex 已知时（单态调用点，编译期可确定目标）
/// </summary>
public sealed class DevirtualizationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "devirtualization";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(
        EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not PhysicalNode.Call call) continue;

            switch (call.dispatch)
            {
                case DispatchKind.dynamic:
                    if (call.witness.HasValue) yield return (node, call with { dispatch = DispatchKind.witness });

                    if (call.method_index.HasValue) yield return (node, call with { dispatch = DispatchKind.@static });

                    break;

                case DispatchKind.witness:
                    if (call.method_index.HasValue) yield return (node, call with { dispatch = DispatchKind.@static });

                    break;
            }
        }
    }
}