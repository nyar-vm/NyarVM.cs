using System.Collections;
using System.Diagnostics;
using Nyar.Types;
using Nyar.VM.NyarVM.Observability;

namespace Nyar.VM.NyarVM.GC;

/// <summary>
///     精确非移动 GC，与 .NET GC 协作
///     使用 Mark-Sweep 算法，精确扫描 GC Roots，不移动对象
///     Value 通过索引引用对象表，Sweep 时只需将不可达条目设为 null
/// </summary>
public sealed class NyarGc : IGcAllocator
{
    #region 写屏障

    /// <summary>
    ///     写屏障：当向老生代对象写入新生代引用时，将目标对象加入记忆集
    ///     在 Minor GC 标记阶段，只需扫描记忆集中的老生代对象，避免全量扫描
    /// </summary>
    /// <param name="targetIndex">被写入的对象表索引（StoreField/SetIndex/StoreGlobal 等操作的目标）。</param>
    /// <param name="newValue">新写入的值。</param>
    public void write_barrier(int targetIndex, Value newValue)
    {
        if (!is_reference_type(newValue)) return;

        var refIndex = extract_object_index(newValue);

        lock (_table_lock)
        {
            if (targetIndex < 0 || targetIndex >= _generations.Count) return;

            if (_generations[targetIndex] != _generation_old) return;

            if (refIndex < 0 || refIndex >= _generations.Count) return;

            if (_generations[refIndex] == _generation_young) _remembered_set.Add(targetIndex);
        }
    }

    #endregion

    #region 字段

    private readonly List<object?> _object_table;
    private readonly object _table_lock;
    private readonly HashSet<int> _free_indices;
    private bool[]? _mark_bits;
    private VmMetrics? _metrics;
    private VmEvents? _events;

    private long _total_allocations;
    private long _total_collections;
    private long _total_reclaimed;

    private const int _default_gc_threshold = 1024;
    private const int _default_young_gc_threshold = 256;

    /// <summary>
    ///     GC 触发阈值：当分配数超过此值时自动触发回收
    /// </summary>
    public int gc_threshold { get; set; } = _default_gc_threshold;

    /// <summary>
    ///     新生代 GC 触发阈值：当新生代分配数超过此值时触发 Minor GC
    /// </summary>
    public int young_gc_threshold { get; set; } = _default_young_gc_threshold;

    #endregion

    #region 分代字段

    /// <summary>
    ///     每个对象槽位的分代标记：0 = 新生代(Young)，1 = 老生代(Old)
    /// </summary>
    private readonly List<byte> _generations;

    /// <summary>
    ///     每个对象槽位的存活年龄（经历的 GC 次数）
    /// </summary>
    private readonly List<byte> _ages;

    /// <summary>
    ///     新生代分配计数（用于触发 Minor GC 的阈值判定）
    /// </summary>
    private long _young_allocations;

    /// <summary>
    ///     Minor GC 执行次数
    /// </summary>
    private long _young_collections;

    /// <summary>
    ///     Major GC（全堆回收）执行次数
    /// </summary>
    private long _major_collections;

    /// <summary>
    ///     晋升到老生代的对象总数
    /// </summary>
    private long _promoted_objects;

    /// <summary>
    ///     记忆集（Remembered Set）：记录了"曾经被写入过新生代引用"的老生代对象索引
    ///     Minor GC 时只需扫描这些对象，无需遍历全部老生代
    /// </summary>
    private readonly HashSet<int> _remembered_set;

    private const byte _generation_young = 0;
    private const byte _generation_old = 1;
    private const byte _max_age = 15;
    private const byte _promotion_age = 3;

    #endregion

    #region 属性

    /// <summary>
    ///     对象表中存活对象数量
    /// </summary>
    public int live_object_count
    {
        get
        {
            lock (_table_lock)
            {
                return _object_table.Count(obj => obj is not null);
            }
        }
    }

    /// <summary>
    ///     对象表总容量（含空槽）
    /// </summary>
    public int table_capacity
    {
        get
        {
            lock (_table_lock)
            {
                return _object_table.Count;
            }
        }
    }

    /// <summary>
    ///     空闲槽位数量
    /// </summary>
    public int free_slot_count => _free_indices.Count;

    /// <summary>
    ///     总分配次数
    /// </summary>
    public long total_allocations => _total_allocations;

    /// <summary>
    ///     总回收次数
    /// </summary>
    public long total_collections => _total_collections;

    /// <summary>
    ///     总回收对象数
    /// </summary>
    public long total_reclaimed => _total_reclaimed;

    /// <summary>
    ///     Minor GC 执行次数
    /// </summary>
    public long young_collections => _young_collections;

    /// <summary>
    ///     Major GC 执行次数
    /// </summary>
    public long major_collections => _major_collections;

    /// <summary>
    ///     晋升到老生代的对象总数
    /// </summary>
    public long promoted_objects => _promoted_objects;

    /// <summary>
    ///     新生代活对象数量
    /// </summary>
    public int young_live_count
    {
        get
        {
            lock (_table_lock)
            {
                var count = 0;
                for (var i = 0; i < _object_table.Count; i++)
                    if (_object_table[i] is not null && i < _generations.Count && _generations[i] == _generation_young)
                        count++;

                return count;
            }
        }
    }

    /// <summary>
    ///     老生代活对象数量
    /// </summary>
    public int old_live_count
    {
        get
        {
            lock (_table_lock)
            {
                var count = 0;
                for (var i = 0; i < _object_table.Count; i++)
                    if (_object_table[i] is not null && i < _generations.Count && _generations[i] == _generation_old)
                        count++;

                return count;
            }
        }
    }

    /// <summary>
    ///     记忆集大小
    /// </summary>
    public int remembered_set_size
    {
        get
        {
            lock (_table_lock)
            {
                return _remembered_set.Count;
            }
        }
    }

    #endregion

    #region 构造函数

    /// <summary>
    ///     初始化 NyarGC
    /// </summary>
    /// <param name="objectTable">共享的对象表引用。</param>
    /// <param name="tableLock">对象表锁。</param>
    public NyarGc(List<object?> objectTable, object tableLock)
    {
        _object_table = objectTable;
        _table_lock = tableLock;
        _free_indices = [];
        _generations = [];
        _ages = [];
        _remembered_set = [];
    }

    /// <summary>
    ///     设置可观测性组件（指标收集器和事件系统）
    /// </summary>
    /// <param name="metrics">指标收集器（可选）。</param>
    /// <param name="events">事件系统（可选）。</param>
    public void set_observability(VmMetrics? metrics, VmEvents? events)
    {
        _metrics = metrics;
        _events = events;
    }

    #endregion

    #region 分配

    /// <summary>
    ///     分配对象表槽位，优先复用空闲槽，新对象分配在新生代
    /// </summary>
    /// <param name="obj">要存储的对象。</param>
    /// <returns>对象表索引。</returns>
    public int allocate(object obj)
    {
        Interlocked.Increment(ref _total_allocations);
        Interlocked.Increment(ref _young_allocations);

        lock (_table_lock)
        {
            if (_free_indices.Count > 0)
            {
                var index = _free_indices.First();
                _free_indices.Remove(index);
                _object_table[index] = obj;
                _generations[index] = _generation_young;
                _ages[index] = 0;
                return index;
            }

            var newIndex = _object_table.Count;
            _object_table.Add(obj);
            _generations.Add(_generation_young);
            _ages.Add(0);
            return newIndex;
        }
    }

    /// <summary>
    ///     释放对象表槽位
    /// </summary>
    /// <param name="index">对象表索引。</param>
    public void free(int index)
    {
        lock (_table_lock)
        {
            if (index >= 0 && index < _object_table.Count && _object_table[index] is not null)
            {
                _object_table[index] = null;
                _free_indices.Add(index);
                _remembered_set.Remove(index);
            }
        }
    }

    #endregion

    #region Mark-Sweep 回收

    /// <summary>
    ///     执行完整的 Mark-Sweep 回收（Major GC，全堆）
    ///     1. Mark：从 GC Roots 出发，标记所有可达对象
    ///     2. Sweep：扫描对象表，回收不可达对象
    /// </summary>
    /// <param name="roots">GC Roots 提供者。</param>
    /// <returns>回收的对象数量。</returns>
    public int collect(IGcRootProvider roots)
    {
        Interlocked.Increment(ref _total_collections);
        Interlocked.Increment(ref _major_collections);

        _events?.OnGcCollectionStarted(true);

        var sw = Stopwatch.StartNew();

        lock (_table_lock)
        {
            var markBits = mark_phase(roots);
            var reclaimed = sweep_phase(markBits);

            Interlocked.Add(ref _total_reclaimed, reclaimed);

            sw.Stop();

            var liveCount = live_object_count;
            _metrics?.record_gc_major_collection(sw.Elapsed.TotalMilliseconds, liveCount, reclaimed);
            _events?.OnGcCollectionCompleted(true, sw.Elapsed.TotalMilliseconds, liveCount, reclaimed);

            return reclaimed;
        }
    }

    /// <summary>
    ///     执行 Minor GC：仅回收新生代对象
    ///     1. Mark：从 GC Roots 出发，标记所有可达对象（含老生代子引用保守扫描）
    ///     2. Promotion：存活新生代对象年龄递增，达到 PromotionAge 则晋升到老生代
    ///     3. Sweep：仅回收未标记的新生代对象
    /// </summary>
    /// <param name="roots">GC Roots 提供者。</param>
    /// <returns>回收的对象数量。</returns>
    public int minor_collect(IGcRootProvider roots)
    {
        Interlocked.Increment(ref _young_collections);

        _events?.OnGcCollectionStarted(false);

        var sw = Stopwatch.StartNew();

        lock (_table_lock)
        {
            var reclaimed = minor_mark_sweep_promote(roots);

            Interlocked.Add(ref _total_reclaimed, reclaimed);

            sw.Stop();

            var liveCount = live_object_count;
            var promoted = _promoted_objects;
            _metrics?.record_gc_minor_collection(sw.Elapsed.TotalMilliseconds, liveCount, reclaimed, promoted);
            _events?.OnGcCollectionCompleted(false, sw.Elapsed.TotalMilliseconds, liveCount, reclaimed, promoted);

            return reclaimed;
        }
    }

    /// <summary>
    ///     Minor GC 核心：标记 → 晋升 → 清扫新生代
    ///     使用记忆集精确定位引用了新生代对象的老生代，避免全量扫描
    /// </summary>
    private int minor_mark_sweep_promote(IGcRootProvider roots)
    {
        var tableSize = _object_table.Count;
        var markBits = new bool[tableSize];
        var rootValues = roots.get_root_values();
        var workQueue = new Queue<int>();

        // 阶段 1a: 从 GC Roots 入队
        foreach (var value in rootValues) enqueue_if_reference(value, markBits, workQueue);

        // 阶段 1b: 从记忆集老生代对象入队子引用
        foreach (var oldIndex in _remembered_set)
        {
            if (oldIndex >= tableSize) continue;

            var obj = _object_table[oldIndex];
            if (obj is null) continue;

            foreach (var childValue in extract_child_values(obj)) enqueue_if_reference(childValue, markBits, workQueue);
        }

        // 阶段 1c: 标记主循环
        while (workQueue.Count > 0)
        {
            var index = workQueue.Dequeue();

            if (index >= tableSize || markBits[index]) continue;

            markBits[index] = true;

            var obj = _object_table[index];
            if (obj is null) continue;

            // 老生代且不在记忆集中：不扫描子引用
            if (_generations[index] == _generation_old && !_remembered_set.Contains(index)) continue;

            foreach (var childValue in extract_child_values(obj)) enqueue_if_reference(childValue, markBits, workQueue);
        }

        // 清空记忆集
        _remembered_set.Clear();

        var promoted = 0;

        // 阶段 2: 晋升 —— 存活新生代对象年龄 +1，达标则晋升到老生代
        for (var i = 0; i < tableSize; i++)
        {
            if (_object_table[i] is null) continue;

            if (_generations[i] != _generation_young || !markBits[i]) continue;

            var newAge = (byte)Math.Min(_ages[i] + 1, _max_age);
            _ages[i] = newAge;

            if (newAge >= _promotion_age)
            {
                _generations[i] = _generation_old;
                promoted++;
            }
        }

        Interlocked.Add(ref _promoted_objects, promoted);

        // 阶段 3: Sweep —— 仅清扫新生代中未标记的对象
        var reclaimed = 0;

        for (var i = 0; i < tableSize; i++)
        {
            if (markBits[i]) continue;

            if (_generations[i] != _generation_young) continue;

            if (_object_table[i] is not null)
            {
                _object_table[i] = null;
                _free_indices.Add(i);
                reclaimed++;
            }
        }

        compact_free_list();

        return reclaimed;
    }

    /// <summary>
    ///     条件触发回收：优先 Minor GC（新生代），必要时 Major GC（全堆）
    /// </summary>
    /// <param name="roots">GC Roots 提供者。</param>
    /// <returns>是否执行了回收。</returns>
    public bool maybe_collect(IGcRootProvider roots)
    {
        var performed = false;

        if (_young_allocations >= young_gc_threshold)
        {
            minor_collect(roots);
            performed = true;

            var youngLive = young_live_count;
            young_gc_threshold = Math.Max(_default_young_gc_threshold, youngLive * 3);
        }

        if (_total_allocations - _total_reclaimed >= gc_threshold)
        {
            collect(roots);
            performed = true;
            gc_threshold = Math.Max(_default_gc_threshold, live_object_count * 2);
        }

        _young_allocations = 0;

        return performed;
    }

    /// <summary>
    ///     Mark 阶段：从 GC Roots 出发，标记所有可达对象
    /// </summary>
    private bool[] mark_phase(IGcRootProvider roots)
    {
        var tableSize = _object_table.Count;
        var markBits = new bool[tableSize];

        var rootValues = roots.get_root_values();

        var workQueue = new Queue<int>();

        foreach (var value in rootValues) enqueue_if_reference(value, markBits, workQueue);

        while (workQueue.Count > 0)
        {
            var index = workQueue.Dequeue();

            if (index >= tableSize || markBits[index]) continue;

            markBits[index] = true;

            var obj = _object_table[index];
            if (obj is null) continue;

            foreach (var childValue in extract_child_values(obj)) enqueue_if_reference(childValue, markBits, workQueue);
        }

        return markBits;
    }

    /// <summary>
    ///     Sweep 阶段：回收不可达对象
    /// </summary>
    private int sweep_phase(bool[] markBits)
    {
        var reclaimed = 0;

        for (var i = 0; i < _object_table.Count; i++)
        {
            if (markBits[i]) continue;

            if (_object_table[i] is not null)
            {
                _object_table[i] = null;
                _free_indices.Add(i);
                reclaimed++;
            }
        }

        compact_free_list();

        return reclaimed;
    }

    /// <summary>
    ///     压缩空闲列表：移除对象表尾部的连续 null 条目，同步裁剪分代追踪列表
    /// </summary>
    private void compact_free_list()
    {
        while (_object_table.Count > 0 && _object_table[^1] is null)
        {
            var lastIndex = _object_table.Count - 1;
            _object_table.RemoveAt(lastIndex);
            _generations.RemoveAt(lastIndex);
            _ages.RemoveAt(lastIndex);
            _free_indices.Remove(lastIndex);
        }
    }

    #endregion

    #region 引用识别

    /// <summary>
    ///     判断 Value 是否为引用类型（需要 GC 跟踪）
    /// </summary>
    public static bool is_reference_type(Value value)
    {
        var bits = value.raw_bits;

        if (bits == 0) return false;

        if ((bits & _nan_tag_mask) != _nan_tag_mask) return false;

        var typeTag = (int)((bits >> _type_tag_shift) & _type_tag_mask);
        return typeTag is _type_tag_object or _type_tag_big_int or _type_tag_string
            or _type_tag_closure or _type_tag_continuation or _type_tag_effect
            or _type_tag_long or _type_tag_witness_table;
    }

    /// <summary>
    ///     从 Value 中提取对象表索引
    /// </summary>
    public static int extract_object_index(Value value)
    {
        return (int)(value.raw_bits & _payload_mask);
    }

    /// <summary>
    ///     如果 Value 是引用类型，将其加入工作队列
    /// </summary>
    private static void enqueue_if_reference(Value value, bool[] markBits, Queue<int> workQueue)
    {
        if (!is_reference_type(value)) return;

        var index = extract_object_index(value);
        if (index < markBits.Length && !markBits[index]) workQueue.Enqueue(index);
    }

    /// <summary>
    ///     从托管对象中提取子 Value 引用
    /// </summary>
    private static IEnumerable<Value> extract_child_values(object obj)
    {
        switch (obj)
        {
            case IDictionary dict:
            {
                foreach (DictionaryEntry entry in dict)
                    if (entry.Value is Value v)
                        yield return v;

                break;
            }

            case IList list:
            {
                foreach (var item in list)
                    if (item is Value v)
                        yield return v;

                break;
            }

            case NyarClosure closure:
            {
                if (closure.upvalues is not null)
                    foreach (var upvalue in closure.upvalues)
                        yield return upvalue;

                break;
            }
        }
    }

    #endregion

    #region 标签常量

    private const ulong _nan_tag_mask = 0xFFF8_0000_0000_0000UL;
    private const int _type_tag_shift = 47;
    private const ulong _type_tag_mask = 0xFUL;
    private const ulong _payload_mask = 0x0000_7FFF_FFFF_FFFFUL;

    private const int _type_tag_object = 3;
    private const int _type_tag_big_int = 4;
    private const int _type_tag_string = 5;
    private const int _type_tag_closure = 6;
    private const int _type_tag_continuation = 7;
    private const int _type_tag_effect = 8;
    private const int _type_tag_long = 9;
    private const int _type_tag_witness_table = 10;

    #endregion
}