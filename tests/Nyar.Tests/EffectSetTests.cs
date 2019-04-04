using Nyar.IR.Intent;

namespace Nyar.Tests;

public sealed class EffectSetTests
{
    [Fact]
    public void Pure_is_empty()
    {
        Assert.True(EffectSet.pure.is_empty);
        Assert.True(EffectSet.pure.is_pure);
        Assert.Empty(EffectSet.pure.effects);
    }

    [Fact]
    public void From_single_effect_has_count_one()
    {
        var es = EffectSet.from(EffectKind.read);

        Assert.Single(es.effects);
        Assert.True(es.contains(EffectKind.read));
    }

    [Fact]
    public void From_multiple_effects_has_correct_count()
    {
        var es = EffectSet.from(EffectKind.read, EffectKind.write);

        Assert.Equal(2, es.effects.Count);
        Assert.True(es.contains(EffectKind.read));
        Assert.True(es.contains(EffectKind.write));
    }

    [Fact]
    public void Constructor_with_IEnumerable_works()
    {
        var list = new List<EffectKind> { EffectKind.io, EffectKind.allocate };
        var es = new EffectSet(list);

        Assert.Equal(2, es.effects.Count);
        Assert.True(es.contains(EffectKind.io));
        Assert.True(es.contains(EffectKind.allocate));
    }

    [Fact]
    public void Union_combines_effects()
    {
        var a = EffectSet.from(EffectKind.read);
        var b = EffectSet.from(EffectKind.write);
        var u = a.union(b);

        Assert.Equal(2, u.effects.Count);
        Assert.True(u.contains(EffectKind.read));
        Assert.True(u.contains(EffectKind.write));
    }

    [Fact]
    public void Union_is_idempotent()
    {
        var a = EffectSet.from(EffectKind.read, EffectKind.write);
        var u = a.union(a);

        Assert.Equal(2, u.effects.Count);
    }

    [Fact]
    public void Union_with_pure_is_identity()
    {
        var a = EffectSet.from(EffectKind.read);
        var u = a.union(EffectSet.pure);

        Assert.Single(u.effects);
        Assert.True(u.contains(EffectKind.read));
    }

    [Fact]
    public void Union_pure_with_pure_is_pure()
    {
        var u = EffectSet.pure.union(EffectSet.pure);

        Assert.True(u.is_pure);
        Assert.True(u.is_empty);
    }

    [Fact]
    public void Difference_removes_effects()
    {
        var a = EffectSet.from(EffectKind.read);
        var b = EffectSet.from(EffectKind.read);
        var d = a.difference(b);

        Assert.True(d.is_empty);
        Assert.False(d.contains(EffectKind.read));
    }

    [Fact]
    public void Difference_preserves_unrelated()
    {
        var a = EffectSet.from(EffectKind.read, EffectKind.write);
        var b = EffectSet.from(EffectKind.read);
        var d = a.difference(b);

        Assert.Single(d.effects);
        Assert.True(d.contains(EffectKind.write));
        Assert.False(d.contains(EffectKind.read));
    }

    [Fact]
    public void Difference_with_pure_is_identity()
    {
        var a = EffectSet.from(EffectKind.read, EffectKind.write);
        var d = a.difference(EffectSet.pure);

        Assert.Equal(2, d.effects.Count);
        Assert.True(d.contains(EffectKind.read));
        Assert.True(d.contains(EffectKind.write));
    }

    [Fact]
    public void Difference_with_superset_returns_empty()
    {
        var a = EffectSet.from(EffectKind.read);
        var b = EffectSet.from(EffectKind.read, EffectKind.write);
        var d = a.difference(b);

        Assert.True(d.is_empty);
    }

    [Fact]
    public void Contains_returns_false_for_missing()
    {
        var es = EffectSet.from(EffectKind.read);

        Assert.False(es.contains(EffectKind.write));
        Assert.False(es.contains(EffectKind.io));
    }

    [Fact]
    public void Contains_returns_false_for_empty_set()
    {
        Assert.False(EffectSet.pure.contains(EffectKind.read));
    }

    [Fact]
    public void Contains_returns_true_for_all_constructed_effects()
    {
        var es = EffectSet.from(EffectKind.read, EffectKind.write, EffectKind.io, EffectKind.allocate);

        Assert.True(es.contains(EffectKind.read));
        Assert.True(es.contains(EffectKind.write));
        Assert.True(es.contains(EffectKind.io));
        Assert.True(es.contains(EffectKind.allocate));
    }

    [Fact]
    public void IsSubsetOf_empty_is_subset_of_everything()
    {
        var a = EffectSet.from(EffectKind.read);

        Assert.True(EffectSet.pure.is_subset_of(a));
        Assert.True(EffectSet.pure.is_subset_of(EffectSet.pure));
    }

    [Fact]
    public void IsSubsetOf_equal_sets()
    {
        var a = EffectSet.from(EffectKind.read, EffectKind.write);
        var b = EffectSet.from(EffectKind.read, EffectKind.write);

        Assert.True(a.is_subset_of(b));
        Assert.True(b.is_subset_of(a));
    }

    [Fact]
    public void IsSubsetOf_smaller_is_subset_of_larger()
    {
        var a = EffectSet.from(EffectKind.read);
        var b = EffectSet.from(EffectKind.read, EffectKind.write);

        Assert.True(a.is_subset_of(b));
        Assert.False(b.is_subset_of(a));
    }

    [Fact]
    public void IsSubsetOf_disjoint_sets()
    {
        var a = EffectSet.from(EffectKind.read);
        var b = EffectSet.from(EffectKind.write);

        Assert.False(a.is_subset_of(b));
        Assert.False(b.is_subset_of(a));
    }

    [Fact]
    public void IsSubsetOf_partial_overlap()
    {
        var a = EffectSet.from(EffectKind.read, EffectKind.write);
        var b = EffectSet.from(EffectKind.write, EffectKind.io);

        Assert.False(a.is_subset_of(b));
        Assert.False(b.is_subset_of(a));
    }

    [Fact]
    public void IsSubsetOf_set_is_subset_of_itself()
    {
        var es = EffectSet.from(EffectKind.read, EffectKind.write, EffectKind.io);

        Assert.True(es.is_subset_of(es));
    }

    [Fact]
    public void Constructor_with_dup_effects_does_not_resize()
    {
        var es = new EffectSet([EffectKind.read, EffectKind.read, EffectKind.read]);

        Assert.Single(es.effects);
    }

    [Fact]
    public void Union_chaining_merges_three_sets()
    {
        var a = EffectSet.from(EffectKind.read);
        var b = EffectSet.from(EffectKind.write);
        var c = EffectSet.from(EffectKind.io);
        var u = a.union(b).union(c);

        Assert.Equal(3, u.effects.Count);
        Assert.True(u.contains(EffectKind.read));
        Assert.True(u.contains(EffectKind.write));
        Assert.True(u.contains(EffectKind.io));
    }

    [Fact]
    public void Union_overlapping_deduplicates()
    {
        var a = EffectSet.from(EffectKind.read, EffectKind.write);
        var b = EffectSet.from(EffectKind.write, EffectKind.io);
        var u = a.union(b);

        Assert.Equal(3, u.effects.Count);
        Assert.True(u.contains(EffectKind.read));
        Assert.True(u.contains(EffectKind.write));
        Assert.True(u.contains(EffectKind.io));
    }

    [Fact]
    public void Union_all_effect_kinds_twice_is_idempotent()
    {
        var allKinds = Enum.GetValues<EffectKind>();
        var a = new EffectSet(allKinds);
        var u = a.union(a);

        Assert.Equal(allKinds.Length, u.effects.Count);
        foreach (var kind in allKinds)
        {
            Assert.True(u.contains(kind));
        }
    }

    [Fact]
    public void Difference_disjoint_sets_returns_original()
    {
        var a = EffectSet.from(EffectKind.read, EffectKind.write);
        var b = EffectSet.from(EffectKind.io, EffectKind.allocate);
        var d = a.difference(b);

        Assert.Equal(2, d.effects.Count);
        Assert.True(d.contains(EffectKind.read));
        Assert.True(d.contains(EffectKind.write));
    }

    [Fact]
    public void Difference_chaining_removes_multiple_sets()
    {
        var a = EffectSet.from(EffectKind.read, EffectKind.write, EffectKind.io, EffectKind.allocate);
        var b = EffectSet.from(EffectKind.read);
        var c = EffectSet.from(EffectKind.write);
        var d = a.difference(b).difference(c);

        Assert.Equal(2, d.effects.Count);
        Assert.True(d.contains(EffectKind.io));
        Assert.True(d.contains(EffectKind.allocate));
        Assert.False(d.contains(EffectKind.read));
        Assert.False(d.contains(EffectKind.write));
    }

    [Fact]
    public void Difference_pure_minus_non_empty_is_pure()
    {
        var a = EffectSet.from(EffectKind.read, EffectKind.write);
        var d = EffectSet.pure.difference(a);

        Assert.True(d.is_empty);
        Assert.True(d.is_pure);
    }

    [Fact]
    public void Difference_partial_overlap_removes_only_common()
    {
        var a = EffectSet.from(EffectKind.read, EffectKind.write, EffectKind.io);
        var b = EffectSet.from(EffectKind.write, EffectKind.allocate);
        var d = a.difference(b);

        Assert.Equal(2, d.effects.Count);
        Assert.True(d.contains(EffectKind.read));
        Assert.True(d.contains(EffectKind.io));
        Assert.False(d.contains(EffectKind.write));
        Assert.False(d.contains(EffectKind.allocate));
    }

    [Fact]
    public void Contains_all_enum_values()
    {
        var allKinds = Enum.GetValues<EffectKind>();
        var es = new EffectSet(allKinds);

        foreach (var kind in allKinds)
        {
            Assert.True(es.contains(kind));
        }
    }

    [Fact]
    public void Contains_any_single_kind_in_full_set()
    {
        var allKinds = Enum.GetValues<EffectKind>();
        var es = new EffectSet(allKinds);

        Assert.True(es.contains(EffectKind.read));
        Assert.True(es.contains(EffectKind.write));
        Assert.True(es.contains(EffectKind.io));
        Assert.True(es.contains(EffectKind.allocate));
        Assert.True(es.contains(EffectKind.free));
        Assert.True(es.contains(EffectKind.@throw));
        Assert.True(es.contains(EffectKind.capture_cc));
        Assert.True(es.contains(EffectKind.choice_point));
        Assert.True(es.contains(EffectKind.perform));
        Assert.True(es.contains(EffectKind.handle));
    }

    [Fact]
    public void Contains_boundary_values()
    {
        var es = EffectSet.from(EffectKind.read, EffectKind.handle);

        Assert.True(es.contains(EffectKind.read));
        Assert.True(es.contains(EffectKind.handle));
        Assert.False(es.contains(EffectKind.write));
    }

    [Fact]
    public void IsSubsetOf_non_empty_is_not_subset_of_empty()
    {
        var a = EffectSet.from(EffectKind.read);
        var b = EffectSet.from(EffectKind.read, EffectKind.write);

        Assert.False(a.is_subset_of(EffectSet.pure));
        Assert.False(b.is_subset_of(EffectSet.pure));
    }

    [Fact]
    public void IsSubsetOf_full_set_contains_all()
    {
        var allKinds = Enum.GetValues<EffectKind>();
        var fullSet = new EffectSet(allKinds);
        var smallSet = EffectSet.from(EffectKind.read, EffectKind.allocate);

        Assert.True(smallSet.is_subset_of(fullSet));
        Assert.True(EffectSet.pure.is_subset_of(fullSet));
        Assert.True(fullSet.is_subset_of(fullSet));
    }

    [Fact]
    public void Empty_set_union_non_empty_yields_non_empty()
    {
        var a = EffectSet.from(EffectKind.io, EffectKind.allocate);
        var u = EffectSet.pure.union(a);

        Assert.Equal(2, u.effects.Count);
        Assert.True(u.contains(EffectKind.io));
        Assert.True(u.contains(EffectKind.allocate));
    }

    [Fact]
    public void Empty_set_difference_non_empty_is_pure()
    {
        var a = EffectSet.from(EffectKind.read);

        Assert.True(EffectSet.pure.difference(a).is_empty);
        Assert.True(EffectSet.pure.difference(a).is_pure);
    }

    [Fact]
    public void Empty_set_is_not_affected_by_any_operation_with_itself()
    {
        Assert.True(EffectSet.pure.union(EffectSet.pure).is_pure);
        Assert.True(EffectSet.pure.difference(EffectSet.pure).is_pure);
        Assert.True(EffectSet.pure.is_subset_of(EffectSet.pure));
        Assert.False(EffectSet.pure.contains(EffectKind.read));
        Assert.True(EffectSet.pure.is_empty);
    }

    [Fact]
    public void Overlapping_union_preserves_both_sides()
    {
        var a = EffectSet.from(EffectKind.read, EffectKind.write, EffectKind.free);
        var b = EffectSet.from(EffectKind.write, EffectKind.allocate, EffectKind.io);
        var u = a.union(b);

        Assert.Equal(5, u.effects.Count);
        Assert.True(u.contains(EffectKind.read));
        Assert.True(u.contains(EffectKind.write));
        Assert.True(u.contains(EffectKind.free));
        Assert.True(u.contains(EffectKind.allocate));
        Assert.True(u.contains(EffectKind.io));
    }

    [Fact]
    public void Overlapping_difference_chain_results_in_empty()
    {
        var a = EffectSet.from(EffectKind.read, EffectKind.write);
        var b = EffectSet.from(EffectKind.read);
        var c = EffectSet.from(EffectKind.write);
        var d = a.difference(b).difference(c);

        Assert.True(d.is_empty);
    }
}