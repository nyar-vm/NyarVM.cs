using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.IR.Rewrite;

/// <summary>
///     Oa 重写规则的抽象基类。
///     子类只需重写 <see cref="apply_to_class" /> 方法（返回 (模式, 替换) 对），
///     基类自动处理遍历所有等价类、Union 操作和 Rebuild。
/// </summary>
public abstract class CommonRewriteRule : IRewriteRule<AlgebraNode>
{
    /// <summary>
    ///     规则名称
    /// </summary>
    public abstract string name { get; }

    /// <summary>
    ///     遍历 E-Graph 中所有等价类，对每个等价类调用 <see cref="egraph" />，
    ///     将产生的替换合并到 E-Graph 中
    /// </summary>
    /// <param name="egraph">目标 E-Graph。</param>
    /// <returns>是否产生了新的等价关系。</returns>
    public bool apply(EGraph<AlgebraNode> egraph)
    {
        var changed = false;
        var classIds = egraph.classes.Keys.ToList();

        foreach (var classId in classIds)
        {
            var id = new Id(classId);
            foreach (var (pattern, replacement) in apply_to_class(egraph, id))
            {
                var patternId = egraph.add(pattern);
                var replacementId = egraph.add(replacement);

                var root1 = egraph.union_find.find(patternId);
                var root2 = egraph.union_find.find(replacementId);

                if (root1 != root2)
                {
                    egraph.union(patternId, replacementId);
                    changed = true;
                }
            }
        }

        if (changed) egraph.rebuild();

        return changed;
    }

    /// <summary>
    ///     在指定等价类上应用规则，返回所有可生成的 (模式, 替换) 对
    /// </summary>
    /// <param name="egraph">目标 E-Graph。</param>
    /// <param name="id">等价类标识符。</param>
    /// <returns>可生成的 (模式, 替换) 对序列。</returns>
    protected abstract IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(
        EGraph<AlgebraNode> egraph, Id id);
}