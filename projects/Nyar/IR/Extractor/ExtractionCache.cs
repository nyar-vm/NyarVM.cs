using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.IR.Extractor;

public sealed class ExtractionCache
{
    private readonly Dictionary<uint, (CostVector Cost, AlgebraNode Node)> _best_cache;
    private readonly Dictionary<uint, AlgebraNode> _node_cache;

    public ExtractionCache()
    {
        _best_cache = new Dictionary<uint, (CostVector, AlgebraNode)>();
        _node_cache = new Dictionary<uint, AlgebraNode>();
    }

    public ExtractionCache(int capacity)
    {
        _best_cache = new Dictionary<uint, (CostVector, AlgebraNode)>(capacity);
        _node_cache = new Dictionary<uint, AlgebraNode>(capacity);
    }

    public int best_count => _best_cache.Count;

    public int node_count => _node_cache.Count;

    public bool try_get_best(uint classId, out CostVector cost, out AlgebraNode node)
    {
        if (_best_cache.TryGetValue(classId, out var entry))
        {
            cost = entry.Cost;
            node = entry.Node;
            return true;
        }

        cost = default;
        node = null!;
        return false;
    }

    public void set_best(uint classId, CostVector cost, AlgebraNode node)
    {
        _best_cache[classId] = (cost, node);
    }

    public bool try_get_node(uint classId, out AlgebraNode node)
    {
        return _node_cache.TryGetValue(classId, out node!);
    }

    public void set_node(uint classId, AlgebraNode node)
    {
        _node_cache[classId] = node;
    }

    public void merge_from(ExtractionCache other)
    {
        foreach (var (classId, entry) in other._best_cache) _best_cache.TryAdd(classId, entry);

        foreach (var (classId, node) in other._node_cache) _node_cache.TryAdd(classId, node);
    }

    public void clear()
    {
        _best_cache.Clear();
        _node_cache.Clear();
    }

    public void invalidate_node(uint classId)
    {
        _node_cache.Remove(classId);
    }

    public void invalidate_all_nodes()
    {
        _node_cache.Clear();
    }
}