using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     布尔简化规则（OA 节点版）：Not(Not(x)) → x，Cmp(Eq, true, x) → x，Cmp(Eq, false, x) → Not(x)，
///     Cmp(Ne, true, x) → Not(x)，Cmp(Ne, false, x) → x，Branch(true, t, f) → t 等。
///     使用 Core 方言 OA 节点 <c>Not</c>、<c>Cmp</c>、<c>CompareOp</c>、<c>Branch</c>、<c>Literal&lt;bool&gt;</c>。
///     此规则需要嵌套等价类遍历和布尔常量检测，无法转换为 <see cref="RewriteRuleAttribute" /> 属性模式。
/// </summary>
public sealed class BooleanSimplificationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "boolean-simplification";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes.ToList())
            switch (node)
            {
                case Not not1:
                {
                    var innerClass = egraph.get_class(not1.operand);
                    if (innerClass is null) break;

                    foreach (var innerNode in innerClass.nodes)
                        if (innerNode is Not innerNot)
                            if (TryGetReplacement(egraph, innerNot.operand, out var rep))
                            {
                                egraph.union(id, innerNot.operand);
                                yield return (node, rep);
                            }

                    break;
                }

                case Cmp { op: CompareOp.eq } cmp:
                {
                    if (TryGetBoolConstant(egraph, cmp.left, out var leftVal))
                    {
                        if (leftVal)
                        {
                            if (TryGetReplacement(egraph, cmp.right, out var rep))
                            {
                                egraph.union(id, cmp.right);
                                yield return (node, rep);
                            }
                        }
                        else
                        {
                            var notNode = new Not(cmp.right);
                            var notId = egraph.add(notNode);
                            egraph.union(id, notId);
                            yield return (node, notNode);
                        }
                    }
                    else if (TryGetBoolConstant(egraph, cmp.right, out var rightVal))
                    {
                        if (rightVal)
                        {
                            if (TryGetReplacement(egraph, cmp.left, out var rep))
                            {
                                egraph.union(id, cmp.left);
                                yield return (node, rep);
                            }
                        }
                        else
                        {
                            var notNode = new Not(cmp.left);
                            var notId = egraph.add(notNode);
                            egraph.union(id, notId);
                            yield return (node, notNode);
                        }
                    }

                    break;
                }

                case Cmp { op: CompareOp.ne } cmp:
                {
                    if (TryGetBoolConstant(egraph, cmp.left, out var leftVal))
                    {
                        if (!leftVal)
                        {
                            if (TryGetReplacement(egraph, cmp.right, out var rep))
                            {
                                egraph.union(id, cmp.right);
                                yield return (node, rep);
                            }
                        }
                        else
                        {
                            var notNode = new Not(cmp.right);
                            var notId = egraph.add(notNode);
                            egraph.union(id, notId);
                            yield return (node, notNode);
                        }
                    }
                    else if (TryGetBoolConstant(egraph, cmp.right, out var rightVal))
                    {
                        if (!rightVal)
                        {
                            if (TryGetReplacement(egraph, cmp.left, out var rep))
                            {
                                egraph.union(id, cmp.left);
                                yield return (node, rep);
                            }
                        }
                        else
                        {
                            var notNode = new Not(cmp.left);
                            var notId = egraph.add(notNode);
                            egraph.union(id, notId);
                            yield return (node, notNode);
                        }
                    }

                    break;
                }

                case Branch branch:
                {
                    if (TryGetBoolConstant(egraph, branch.condition, out var condVal))
                    {
                        var targetId = condVal ? branch.trueLabel : branch.falseLabel;
                        if (TryGetReplacement(egraph, targetId, out var rep))
                        {
                            egraph.union(id, targetId);
                            yield return (node, rep);
                        }
                    }

                    break;
                }
            }
    }

    /// <summary>
    ///     从指定 Id 的等价类中获取替换节点，直接使用等价类中的现有节点以触发 EGraph 合并
    /// </summary>
    private static bool TryGetReplacement(EGraph<AlgebraNode> egraph, Id id, out AlgebraNode replacement)
    {
        replacement = null!;
        var targetClass = egraph.get_class(id);
        if (targetClass is null || targetClass.nodes.Count == 0) return false;

        // 优先返回 OA 节点，次选旧类型
        replacement = targetClass.nodes.FirstOrDefault(n => n is Literal<long>)
                      ?? targetClass.nodes.First();
        return true;
    }

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