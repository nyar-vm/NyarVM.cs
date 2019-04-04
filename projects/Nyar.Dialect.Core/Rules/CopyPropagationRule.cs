using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     复制传播规则（OA 节点版）
/// </summary>
public sealed class CopyPropagationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "copy-propagation";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes.ToList())
            switch (node)
            {
                case Store store:
                {
                    var valueClass = egraph.get_class(store.value);
                    if (valueClass is null) break;

                    foreach (var valueNode in valueClass.nodes)
                        if (valueNode is Load load && AreSameRoot(egraph, load.pointer, store.pointer))
                            yield return (node, store with { value = load.pointer });

                    break;
                }

                case Add add:
                {
                    var leftClass = egraph.get_class(add.left);
                    var rightClass = egraph.get_class(add.right);

                    if (leftClass is not null)
                        foreach (var leftNode in leftClass.nodes)
                            if (leftNode is Add innerAdd && AreSameRoot(egraph, innerAdd.right, add.right))
                                yield return (node, new Add(innerAdd.left, add.right));

                    if (rightClass is not null)
                        foreach (var rightNode in rightClass.nodes)
                            if (rightNode is Add innerAdd && AreSameRoot(egraph, innerAdd.left, add.left))
                                yield return (node, new Add(add.left, innerAdd.right));

                    break;
                }

                case Sub sub:
                {
                    var leftClass = egraph.get_class(sub.left);
                    if (leftClass is not null)
                        foreach (var leftNode in leftClass.nodes)
                            if (leftNode is Add innerAdd && AreSameRoot(egraph, innerAdd.right, sub.right))
                                // 从被减左侧子表达式获取替换节点，触发 EGraph 合并
                                if (TryGetReplacement(egraph, innerAdd.left, out var rep))
                                    yield return (sub, rep);

                    break;
                }
            }
    }

    /// <summary>
    ///     从指定 Id 的等价类中获取替换节点，优先 OA 节点
    /// </summary>
    private static bool TryGetReplacement(EGraph<AlgebraNode> egraph, Id id, out AlgebraNode replacement)
    {
        replacement = null!;
        var targetClass = egraph.get_class(id);
        if (targetClass is null || targetClass.nodes.Count == 0) return false;

        replacement = targetClass.nodes.FirstOrDefault(n => n is Literal<long>)
                      ?? targetClass.nodes.First();
        return true;
    }

    private static bool AreSameRoot(EGraph<AlgebraNode> egraph, Id a, Id b)
    {
        return egraph.union_find.find(a).value == egraph.union_find.find(b).value;
    }
}