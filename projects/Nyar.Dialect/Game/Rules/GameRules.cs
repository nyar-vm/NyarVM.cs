using Nyar.Dialect.Game.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Game.Rules;

/// <summary>
///     序列幂等律：Sequence(a, Sequence(b, c)) == Sequence(a, b, c)
/// </summary>
public sealed class SequenceFlattenRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "sequence-flatten";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Sequence outerSeq) continue;

            var flattened = new List<Id>();
            var changed = false;

            foreach (var child in outerSeq.children)
            {
                var childClass = egraph.get_class(child);
                if (childClass is null)
                {
                    flattened.Add(child);
                    continue;
                }

                var foundInner = false;
                foreach (var childNode in childClass.nodes)
                    if (childNode is Sequence innerSeq)
                    {
                        flattened.AddRange(innerSeq.children);
                        foundInner = true;
                        changed = true;
                        break;
                    }

                if (!foundInner) flattened.Add(child);
            }

            if (changed) yield return (node, new Sequence(flattened));
        }
    }
}

/// <summary>
///     选择幂等律：Selector(a, Selector(b, c)) == Selector(a, b, c)
/// </summary>
public sealed class SelectorFlattenRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "selector-flatten";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Selector outerSel) continue;

            var flattened = new List<Id>();
            var changed = false;

            foreach (var child in outerSel.children)
            {
                var childClass = egraph.get_class(child);
                if (childClass is null)
                {
                    flattened.Add(child);
                    continue;
                }

                var foundInner = false;
                foreach (var childNode in childClass.nodes)
                    if (childNode is Selector innerSel)
                    {
                        flattened.AddRange(innerSel.children);
                        foundInner = true;
                        changed = true;
                        break;
                    }

                if (!foundInner) flattened.Add(child);
            }

            if (changed) yield return (node, new Selector(flattened));
        }
    }
}

/// <summary>
///     双重否定消除：Invert(Invert(a)) == a
/// </summary>
public sealed class DecoratorInvertCancelRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "decorator-invert-cancel";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Decorator { type: DecoratorType.Invert } outer) continue;

            var childClass = egraph.get_class(outer.child);
            if (childClass is null) continue;

            foreach (var childNode in childClass.nodes)
                if (childNode is Decorator { type: DecoratorType.Invert } inner)
                    yield return (node, new Decorator(DecoratorType.Invert, inner.child, null));
        }
    }
}

/// <summary>
///     效用选择优化：UtilitySelect([UtilitySelect(a, b), c]) == UtilitySelect(a, b, c)
/// </summary>
public sealed class UtilitySelectFlattenRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "utility-select-flatten";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not UtilitySelect outer) continue;

            var flattened = new List<Id>();
            var changed = false;

            foreach (var option in outer.options)
            {
                var optionClass = egraph.get_class(option);
                if (optionClass is null)
                {
                    flattened.Add(option);
                    continue;
                }

                var foundInner = false;
                foreach (var optionNode in optionClass.nodes)
                    if (optionNode is UtilitySelect inner)
                    {
                        flattened.AddRange(inner.options);
                        foundInner = true;
                        changed = true;
                        break;
                    }

                if (!foundInner) flattened.Add(option);
            }

            if (changed) yield return (node, new UtilitySelect(flattened));
        }
    }
}