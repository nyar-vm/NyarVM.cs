using System.Collections.Immutable;

namespace Nyar.IR.Intent;

/// <summary>
///     效应集合，表示一个可调用项可能抛出的效应类型集合。
///     纯计算（无效应）对应空集。
/// </summary>
public sealed record EffectSet
{
    /// <summary>
    ///     纯计算效应集合（空集）。
    /// </summary>
    public static readonly EffectSet pure = new([]);

    /// <summary>
    ///     使用效应枚举序列构造效应集合。
    /// </summary>
    /// <param name="effects">效应枚举序列</param>
    public EffectSet(IEnumerable<EffectKind> effects)
    {
        this.effects = ImmutableHashSet.CreateRange(effects);
    }

    /// <summary>
    ///     效应集合的不可变存储。
    /// </summary>
    public ImmutableHashSet<EffectKind> effects { get; init; }

    /// <summary>
    ///     是否为纯计算（效应集合为空）。
    /// </summary>
    public bool is_pure => effects.IsEmpty;

    /// <summary>
    ///     效应集合是否为空。
    /// </summary>
    public bool is_empty => effects.IsEmpty;

    /// <summary>
    ///     计算两个效应集合的并集。
    /// </summary>
    /// <param name="other">另一个效应集合</param>
    /// <returns>包含两个集合中所有效应类型的新效应集合</returns>
    public EffectSet union(EffectSet other)
    {
        return new EffectSet(effects.Union(other.effects));
    }

    /// <summary>
    ///     计算效应集合的差集（当前集合减去 <paramref name="other" />）。
    /// </summary>
    /// <param name="other">要从当前集合中减去的效应集合</param>
    /// <returns>包含在当前集合中但不在 <paramref name="other" /> 中的效应类型的新效应集合</returns>
    public EffectSet difference(EffectSet other)
    {
        return new EffectSet(effects.Except(other.effects));
    }

    /// <summary>
    ///     检查效应集合是否包含指定的效应类型。
    /// </summary>
    /// <param name="effect">要检查的效应类型</param>
    /// <returns>如果包含则返回 <c>true</c></returns>
    public bool contains(EffectKind effect)
    {
        return effects.Contains(effect);
    }

    /// <summary>
    ///     检查当前效应集合是否为另一个效应集合的子集（子效应关系）。
    /// </summary>
    /// <param name="other">另一个效应集合</param>
    /// <returns>
    ///     如果当前集合的所有效应类型都包含在 <paramref name="other" /> 中则返回 <c>true</c>。
    ///     纯计算（空集）是任意效应集合的子集。
    /// </returns>
    public bool is_subset_of(EffectSet other)
    {
        return effects.IsSubsetOf(other.effects);
    }

    /// <summary>
    ///     从效应类型参数列表创建效应集合。
    /// </summary>
    /// <param name="effects">效应类型参数列表</param>
    /// <returns>包含指定效应类型的新效应集合</returns>
    public static EffectSet from(params EffectKind[] effects)
    {
        return new EffectSet(effects);
    }
}