using System.Text;
using Nyar.IR.Intent;

namespace Nyar.EGraph;

/// <summary>
///     E-Graph 等价类图数据结构，支持非破坏性等价变换
/// </summary>
/// <typeparam name="T">语言节点类型</typeparam>
public class EGraph<T> where T : ILanguage<T>
{
    private readonly IAnalysis<T>? _analysis;
    private readonly Dictionary<uint, EClass<T, object>> _classes;
    private readonly HashSet<uint> _dirty;
    private readonly Dictionary<T, Id> _memorize;
    private readonly Dictionary<uint, HashSet<uint>> _parents;
    private uint _next_id;

    /// <summary>
    ///     创建空的 E-Graph
    /// </summary>
    public EGraph() : this(null)
    {
    }

    /// <summary>
    ///     创建带分析的 E-Graph
    /// </summary>
    /// <param name="analysis">分析器。</param>
    /// <param name="comparer">节点相等比较器，用于正确处理集合类型的结构化相等。</param>
    /// <param name="initialCapacity">初始容量提示，用于预分配内部数据结构。</param>
    public EGraph(IAnalysis<T>? analysis, IEqualityComparer<T>? comparer = null, int initialCapacity = 0)
    {
        var ufCapacity = Math.Max(initialCapacity, 64);
        union_find = new UnionFind(ufCapacity);
        var comparerToUse = comparer ?? EqualityComparer<T>.Default;
        _memorize = initialCapacity > 0
            ? new Dictionary<T, Id>(initialCapacity, comparerToUse)
            : new Dictionary<T, Id>(comparerToUse);
        _classes = initialCapacity > 0
            ? new Dictionary<uint, EClass<T, object>>(initialCapacity)
            : new Dictionary<uint, EClass<T, object>>();
        _dirty = [];
        _parents = initialCapacity > 0
            ? new Dictionary<uint, HashSet<uint>>(initialCapacity)
            : new Dictionary<uint, HashSet<uint>>();
        _analysis = analysis;
        _next_id = 1;
    }

    /// <summary>
    ///     并查集
    /// </summary>
    public UnionFind union_find { get; }

    /// <summary>
    ///     所有等价类
    /// </summary>
    public IReadOnlyDictionary<uint, EClass<T, object>> classes => _classes;

    /// <summary>
    ///     添加节点到 E-Graph，自动规范化子节点 Id
    /// </summary>
    /// <param name="enode">要添加的节点。</param>
    /// <returns>节点所属等价类的标识符。</returns>
    public Id add(T enode)
    {
        var canonical = enode.map_children(id => union_find.find(id));

        if (_memorize.TryGetValue(canonical, out var existingId)) return union_find.find(existingId);

        var id = new Id(_next_id++);
        union_find.register(id);

        var data = _analysis?.make(this, canonical);

        _memorize[canonical] = id;
        var eclass = new EClass<T, object>(id, data);
        eclass.nodes.Add(canonical);
        _classes[id.value] = eclass;

        foreach (var child in canonical.child_ids())
        {
            var childRoot = union_find.find(child);
            if (!_parents.TryGetValue(childRoot.value, out var parentSet))
            {
                parentSet = [];
                _parents[childRoot.value] = parentSet;
            }

            parentSet.Add(id.value);
        }

        return id;
    }

    /// <summary>
    ///     批量添加多个节点到 E-Graph，减少中间哈希计算开销
    /// </summary>
    /// <param name="nodes">要添加的节点集合。</param>
    /// <returns>各节点对应的等价类 Id 列表。</returns>
    public List<Id> add_range(IEnumerable<T> nodes)
    {
        var results = new List<Id>();
        foreach (var node in nodes) results.Add(add(node));

        return results;
    }

    /// <summary>
    ///     合并两个等价类
    /// </summary>
    /// <param name="id1">第一个等价类标识符。</param>
    /// <param name="id2">第二个等价类标识符。</param>
    /// <returns>合并后的根节点标识符。</returns>
    public Id union(Id id1, Id id2)
    {
        var root1 = union_find.find(id1);
        var root2 = union_find.find(id2);
        if (root1 == root2) return root1;

        if (!_classes.ContainsKey(root1.value) || !_classes.TryGetValue(root2.value, out var @class)) return root1;

        if (_analysis is not null)
        {
            var data1 = _classes[root1.value].data;
            var data2 = @class.data;
            if (!_analysis.is_compatible(data1, data2)) return root1;
        }

        var newRoot = union_find.union(root1, root2);
        var oldRoot = newRoot == root1 ? root2 : root1;

        _dirty.Add(newRoot.value);

        if (_classes.Remove(oldRoot.value, out var oldClass))
            if (_classes.TryGetValue(newRoot.value, out var newClass))
            {
                newClass.nodes.AddRange(oldClass.nodes);

                if (_analysis is not null)
                {
                    var to = newClass.data;
                    _analysis.merge(ref to, oldClass.data);
                    newClass.data = to;
                }
            }

        if (_parents.TryGetValue(oldRoot.value, out var oldParents))
        {
            if (!_parents.TryGetValue(newRoot.value, out var newParents))
            {
                newParents = [];
                _parents[newRoot.value] = newParents;
            }

            newParents.UnionWith(oldParents);
            _parents.Remove(oldRoot.value);
        }

        return newRoot;
    }

    /// <summary>
    ///     重建 E-Graph 不变式，仅处理脏等价类及其父节点（Worklist 优化）
    /// </summary>
    public void rebuild()
    {
        var worklist = new Queue<uint>(_dirty);
        _dirty.Clear();

        while (worklist.Count > 0)
        {
            var todo = new List<(Id, Id)>();
            var processed = new HashSet<uint>();

            while (worklist.Count > 0)
            {
                var classId = worklist.Dequeue();
                if (!processed.Add(classId)) continue;

                if (!_classes.TryGetValue(classId, out var eclass)) continue;

                var newNodes = new List<T>(eclass.nodes.Count);
                foreach (var node in eclass.nodes)
                {
                    var canonical = node.map_children(child => union_find.find(child));

                    if (_memorize.TryGetValue(canonical, out var existingId))
                    {
                        var existingRoot = union_find.find(existingId);
                        var currentRoot = union_find.find(new Id(classId));
                        if (existingRoot != currentRoot) todo.Add((existingRoot, currentRoot));
                    }
                    else
                    {
                        _memorize.Remove(node);
                        _memorize[canonical] = new Id(classId);
                    }

                    newNodes.Add(canonical);
                }

                eclass.nodes.Clear();
                eclass.nodes.AddRange(newNodes);
            }

            foreach (var (id1, id2) in todo)
            {
                var result = union(id1, id2);
                mark_parents_dirty(result, worklist, processed);
            }

            foreach (var dirtyId in _dirty)
                if (!processed.Contains(dirtyId))
                    worklist.Enqueue(dirtyId);

            _dirty.Clear();
        }
    }

    /// <summary>
    ///     获取等价类
    /// </summary>
    /// <param name="id">等价类标识符。</param>
    /// <returns>等价类实例，若不存在则返回 null。</returns>
    public EClass<T, object>? get_class(Id id)
    {
        var root = union_find.find(id);
        return _classes.GetValueOrDefault(root.value);
    }

    /// <summary>
    ///     导出 EGraph 为 Graphviz DOT 格式，用于调试可视化。
    ///     等价类以矩形节点表示，包含其中所有等价节点。
    ///     eclass 之间的父子关系以有向边表示。
    /// </summary>
    /// <param name="nodeFormatter">节点格式化函数，用于生成节点标签。默认使用 ToString()。</param>
    /// <returns>DOT 格式字符串。</returns>
    public string to_dot(Func<T, string>? nodeFormatter = null)
    {
        var formatter = nodeFormatter ?? (n => n.ToString() ?? "null");
        var sb = new StringBuilder();
        sb.AppendLine("digraph EGraph {");
        sb.AppendLine("    rankdir=TB;");
        sb.AppendLine("    node [shape=record, style=filled, fillcolor=lightyellow];");

        foreach (var (classId, eclass) in _classes)
        {
            var root = union_find.find(new Id(classId));
            var isRoot = root.value == classId;
            var color = isRoot ? "lightyellow" : "lightgray";
            sb.Append($"    c{classId} [label=\"{{eclass {classId}");
            if (!isRoot) sb.Append($" → {root.value}");

            sb.Append("|");
            var first = true;
            foreach (var node in eclass.nodes)
            {
                if (!first) sb.Append("\\n");

                first = false;
                var label = formatter(node).Replace("\"", "\\\"");
                sb.Append(label);
            }

            sb.AppendLine("}\", fillcolor=" + color + "];");

            foreach (var node in eclass.nodes)
            foreach (var child in node.child_ids())
            {
                var childRoot = union_find.find(child);
                sb.AppendLine($"    c{classId} -> c{childRoot.value};");
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    ///     将等价类的所有父节点加入 worklist
    /// </summary>
    private void mark_parents_dirty(Id classId, Queue<uint> worklist, HashSet<uint> processed)
    {
        var root = union_find.find(classId);
        if (_parents.TryGetValue(root.value, out var parentSet))
            foreach (var parentId in parentSet)
            {
                var parentRoot = union_find.find(new Id(parentId));
                if (!processed.Contains(parentRoot.value)) worklist.Enqueue(parentRoot.value);
            }
    }

    /// <summary>
    ///     从 OA 工厂构建的 EGraph 中提取指定等价类的节点
    /// </summary>
    /// <param name="id">等价类标识符。</param>
    /// <returns>等价类中的节点列表。</returns>
    public IReadOnlyList<T> get_nodes(Id id)
    {
        var root = union_find.find(id);
        if (_classes.TryGetValue(root.value, out var eclass)) return eclass.nodes;

        return [];
    }

    /// <summary>
    ///     获取指定等价类的数据
    /// </summary>
    /// <param name="id">等价类标识符。</param>
    /// <returns>等价类数据，若不存在则返回 null。</returns>
    public EClass<T, object>? get_e_class(Id id)
    {
        var root = union_find.find(id);
        return _classes.GetValueOrDefault(root.value);
    }

    /// <summary>
    ///     仅保留指定集合中的等价类，删除其余所有类及其关联内部状态。
    ///     用于死代码消除等需要剪枝的场景。
    /// </summary>
    /// <param name="keepIds">需要保留的等价类标识符集合。</param>
    public void retain_classes(HashSet<uint> keepIds)
    {
        var toRemove = new List<uint>();
        foreach (var classId in _classes.Keys)
            if (!keepIds.Contains(classId))
                toRemove.Add(classId);

        foreach (var classId in toRemove)
        {
            _classes.Remove(classId);
            _dirty.Remove(classId);
            _parents.Remove(classId);
        }

        // 清理 _parents 中对已删除等价类的引用
        foreach (var parentSet in _parents.Values) parentSet.RemoveWhere(id => !_classes.ContainsKey(id));

        // 清理 _memo 中映射到已删除等价类的条目
        var memoToRemove = _memorize
            .Where(kv => !_classes.ContainsKey(kv.Value.value))
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in memoToRemove) _memorize.Remove(key);
    }
}