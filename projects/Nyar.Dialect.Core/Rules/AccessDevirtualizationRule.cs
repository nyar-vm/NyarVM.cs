using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Physical;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core;

/// <summary>
///     字段访问去虚拟化规则：将动态访问降级为低开销访问
///     Dynamic → Witness：当 Witness 引用可用时（IC 反馈已建立见证表映射）
///     Dynamic/Witness → Static：当 FieldIndex > 0 时（编译期可确定字段偏移量）
/// </summary>
public sealed class AccessDevirtualizationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "access-devirtualization";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(
        EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not PhysicalNode.Access access) continue;

            switch (access.dispatch)
            {
                case DispatchKind.dynamic:
                    if (access.witness.HasValue) yield return (node, access with { dispatch = DispatchKind.witness });

                    if (access.field_index > 0) yield return (node, access with { dispatch = DispatchKind.@static });

                    break;

                case DispatchKind.witness:
                    if (access.field_index > 0) yield return (node, access with { dispatch = DispatchKind.@static });

                    break;
            }
        }
    }
}