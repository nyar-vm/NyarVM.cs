using System.Diagnostics;
using System.Text;
using Nyar.Analyzer.ModuleSystem;
using Nyar.Types;
using Nyar.VM.NyarVM.Observability;
using Std.Data.Binary.NyarIR.Data;
using ValueType = Nyar.Types.ValueType;

namespace Nyar.VM.NyarVM.Jit;

/// <summary>
///     去虚拟化决策枚举
/// </summary>
public enum DevirtualizationDecision
{
    /// <summary>
    ///     无需操作
    /// </summary>
    no_action,

    /// <summary>
    ///     提升为静态调用（单态：仅 1 种接收者类型）
    /// </summary>
    promote_to_static,

    /// <summary>
    ///     提升为 Witness 调用（寡态：2-3 种接收者类型）
    /// </summary>
    promote_to_witness,

    /// <summary>
    ///     保持动态调用（超多态：4+ 种接收者类型）
    /// </summary>
    keep_dynamic
}

/// <summary>
///     内联缓存类型键，组合 ValueType 和对象运行时类型哈希
///     解决所有 Object 类型共享同一 ValueType 键的问题
/// </summary>
internal readonly struct IcTypeKey : IEquatable<IcTypeKey>
{
    /// <summary>
    ///     值类型标签
    /// </summary>
    public readonly byte value_type_tag;

    /// <summary>
    ///     对象运行时类型哈希（仅对 Object 类型有意义，其他类型为 0）
    /// </summary>
    public readonly int type_hash;

    /// <summary>
    ///     创建 IC 类型键
    /// </summary>
    public IcTypeKey(byte valueTypeTag, int typeHash)
    {
        value_type_tag = valueTypeTag;
        type_hash = typeHash;
    }

    public bool Equals(IcTypeKey other)
    {
        return value_type_tag == other.value_type_tag && type_hash == other.type_hash;
    }

    public override bool Equals(object? obj)
    {
        return obj is IcTypeKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(value_type_tag, type_hash);
    }

    public static bool operator ==(IcTypeKey left, IcTypeKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(IcTypeKey left, IcTypeKey right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
///     内联缓存槽，缓存最多 4 个类型→目标映射
///     单态时仅 1 个条目（1 次比较），寡态时 2-4 个条目（线性扫描）
///     超过 4 种类型视为超多态，回退慢路径
/// </summary>
internal sealed class InlineCacheSlot
{
    /// <summary>
    ///     最大缓存条目数（超过此数量视为超多态）
    /// </summary>
    private const int _max_entries = 4;

    /// <summary>
    ///     缓存条目数组
    /// </summary>
    private readonly (IcTypeKey Key, int Target)[] _entries = new (IcTypeKey, int)[_max_entries];

    /// <summary>
    ///     当前条目数
    /// </summary>
    private int _count;

    /// <summary>
    ///     是否为空
    /// </summary>
    public bool is_empty => _count == 0;

    /// <summary>
    ///     查找缓存
    /// </summary>
    public int lookup(IcTypeKey key)
    {
        for (var i = 0; i < _count; i++)
            if (_entries[i].Key == key)
                return _entries[i].Target;

        return -1;
    }

    /// <summary>
    ///     更新缓存
    /// </summary>
    public void update(IcTypeKey key, int target)
    {
        for (var i = 0; i < _count; i++)
            if (_entries[i].Key == key)
            {
                _entries[i].Target = target;
                return;
            }

        if (_count < _max_entries)
        {
            _entries[_count] = (key, target);
            _count++;
        }
        else
        {
            _entries[0] = (key, target);
        }
    }

    /// <summary>
    ///     移除指定类型的缓存条目
    /// </summary>
    public bool remove(IcTypeKey key)
    {
        for (var i = 0; i < _count; i++)
            if (_entries[i].Key == key)
            {
                _entries[i] = _entries[_count - 1];
                _entries[_count - 1] = default;
                _count--;
                return true;
            }

        return false;
    }

    /// <summary>
    ///     清空缓存
    /// </summary>
    public void clear()
    {
        Array.Clear(_entries);
        _count = 0;
    }
}

/// <summary>
///     调用点内联缓存分析档案
/// </summary>
public sealed class CallSiteProfile
{
    /// <summary>
    ///     总调用次数
    /// </summary>
    public long total_calls { get; set; }

    /// <summary>
    ///     各类型调用次数
    /// </summary>
    public Dictionary<byte, long> type_counts { get; } = new();

    /// <summary>
    ///     是否已完成去虚拟化
    /// </summary>
    public bool is_devirtualized { get; set; }
}

/// <summary>
///     JIT 编译器：热点检测 + IL 发射编译 + 内联缓存 + 去虚拟化
/// </summary>
public sealed class JitCompiler
{
    #region 字段

    private readonly JitEmitCompiler _emit_compiler = new();
    private readonly Dictionary<int, long> _call_counts = new();
    private readonly Dictionary<int, JitCompiledFunction> _compiled_functions = new();
    private readonly Dictionary<int, InlineCacheSlot> _ic_slots = new();
    private readonly Dictionary<int, int> _cache_slot_pc_map = new();
    private readonly Dictionary<int, CallSiteProfile> _call_site_profiles = new();
    private readonly Dictionary<int, int> _inline_cache_versions = new();
    private readonly Dictionary<int, byte[]> _original_code_bytes_backup = new();

    private byte[]? _patchable_code_bytes;
    private byte[]? _code_bytes;

    /// <summary>
    ///     当前模块（供 JitCallHelper 访问）
    /// </summary>
    internal IModule? _module { get; private set; }

    /// <summary>
    ///     当前堆内存（供 JitCallHelper 内存操作访问）
    /// </summary>
    internal NyarHeap? _heap { get; private set; }

    private Func<int, Value[], Value>? _interpreter;
    private Func<bool>? _is_debugger_attached;
    private IJitCache? _cache;
    private long _ic_hit_count;
    private long _ic_miss_count;
    private VmMetrics? _metrics;
    private VmEvents? _events;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建 JIT 编译器（默认选项）
    /// </summary>
    public JitCompiler() : this(new JitOptions())
    {
    }

    /// <summary>
    ///     创建 JIT 编译器（自定义选项）
    /// </summary>
    public JitCompiler(JitOptions options)
    {
        this.options = options;
    }

    #endregion

    #region 属性

    /// <summary>
    ///     JIT 编译器配置选项
    /// </summary>
    public JitOptions options { get; }

    /// <summary>
    ///     总编译次数
    /// </summary>
    public int total_compilations { get; private set; }

    /// <summary>
    ///     已编译函数数量
    /// </summary>
    public int compiled_function_count => _compiled_functions.Count;

    /// <summary>
    ///     IC 命中次数
    /// </summary>
    public long ic_hit_count => Volatile.Read(ref _ic_hit_count);

    /// <summary>
    ///     IC 未命中次数
    /// </summary>
    public long ic_miss_count => Volatile.Read(ref _ic_miss_count);

    /// <summary>
    ///     IC 命中率（0.0 ~ 1.0），无数据时返回 0
    /// </summary>
    public double ic_hit_rate
    {
        get
        {
            var total = ic_hit_count + ic_miss_count;
            return total == 0 ? 0.0 : (double)ic_hit_count / total;
        }
    }

    #endregion

    #region 上下文设置

    /// <summary>
    ///     设置当前模块的代码字节流上下文。
    /// </summary>
    public void set_context(byte[] codeBytes, IModule module)
    {
        _code_bytes = codeBytes;
        _module = module;
    }

    /// <summary>
    ///     设置堆内存引用（供 JitCallHelper 内存操作使用）
    /// </summary>
    public void set_heap(NyarHeap heap)
    {
        _heap = heap;
    }

    /// <summary>
    ///     设置解释执行回调
    /// </summary>
    public void set_interpreter(Func<int, Value[], Value> interpretFunction)
    {
        _interpreter = interpretFunction;
    }

    /// <summary>
    ///     设置调试器检查回调
    /// </summary>
    public void set_debugger_check(Func<bool> check)
    {
        _is_debugger_attached = check;
    }

    /// <summary>
    ///     设置可修补的代码字节流引用。
    /// </summary>
    public void set_patchable_bytecode(byte[] codeBytes)
    {
        _patchable_code_bytes = codeBytes;
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

    #region 热点检测与编译

    /// <summary>
    ///     记录函数调用，达到热点阈值时触发 JIT 编译
    /// </summary>
    /// <param name="funcIndex">函数索引。</param>
    /// <returns>true 表示本次触发了 JIT 编译。</returns>
    public bool record_call(int funcIndex)
    {
        var count = _call_counts.GetValueOrDefault(funcIndex, 0);

        count++;
        _call_counts[funcIndex] = count;

        if (!options.enabled || !options.tiered_compilation) return false;

        if (_is_debugger_attached != null && _is_debugger_attached()) return false;

        if (count == options.hot_threshold) return try_compile(funcIndex);

        return false;
    }

    /// <summary>
    ///     强制编译指定函数（不经过热点阈值）
    /// </summary>
    public bool force_compile(int funcIndex)
    {
        if (!options.enabled) return false;

        if (_compiled_functions.ContainsKey(funcIndex)) return false;

        return try_compile(funcIndex);
    }

    /// <summary>
    ///     检查函数是否已编译
    /// </summary>
    public bool is_compiled(int funcIndex)
    {
        return _compiled_functions.ContainsKey(funcIndex);
    }

    /// <summary>
    ///     获取已编译函数
    /// </summary>
    public JitCompiledFunction? get_compiled_function(int funcIndex)
    {
        return _compiled_functions.GetValueOrDefault(funcIndex);
    }

    /// <summary>
    ///     获取函数调用计数
    /// </summary>
    public int get_call_count(int funcIndex)
    {
        return _call_counts.TryGetValue(funcIndex, out var count) ? (int)count : 0;
    }

    #endregion

    #region 缓存管理

    /// <summary>
    ///     设置 JIT 编译结果持久化缓存
    /// </summary>
    public void set_cache(IJitCache? cache)
    {
        _cache = cache;
    }

    /// <summary>
    ///     使用 IJitCache 预热历史热点函数
    /// </summary>
    public void warmup()
    {
        if (_cache == null || _module == null) return;

        var moduleName = _module.name ?? "";
        var functions = _cache.get_previously_compiled_functions(moduleName);

        foreach (var funcIndex in functions)
            if (funcIndex >= 0 && funcIndex < _module.functions.Count)
                _call_counts[funcIndex] = options.hot_threshold;
    }

    /// <summary>
    ///     清除已编译函数缓存（保留调用计数）
    /// </summary>
    public void clear_cache()
    {
        _compiled_functions.Clear();
    }

    /// <summary>
    ///     重置调用计数
    /// </summary>
    public void reset_call_counts()
    {
        _call_counts.Clear();
    }

    /// <summary>
    ///     获取统计信息字符串
    /// </summary>
    public string get_statistics()
    {
        var sb = new StringBuilder();
        sb.AppendLine("JIT 编译器统计:");
        sb.AppendLine($"  编译函数: {_compiled_functions.Count}");
        sb.AppendLine($"  总编译次数: {total_compilations}");
        sb.AppendLine($"  IC 命中: {ic_hit_count}");
        sb.AppendLine($"  IC 未命中: {ic_miss_count}");
        sb.AppendLine($"  IC 命中率: {ic_hit_rate:P1}");
        return sb.ToString();
    }

    #endregion

    #region 内联缓存

    /// <summary>
    ///     注册缓存槽到 PC 偏移的映射
    /// </summary>
    public void register_cache_slot_pc(int cacheSlot, int pc)
    {
        _cache_slot_pc_map[cacheSlot] = pc;
    }

    /// <summary>
    ///     查找内联缓存：根据缓存槽和接收者查找已缓存的函数索引
    ///     使用 IcTypeKey（ValueType + 对象运行时类型哈希）作为缓存键
    /// </summary>
    public int lookup_inline_cache(int cacheSlot, Value receiver)
    {
        if (!_ic_slots.TryGetValue(cacheSlot, out var slot))
        {
            Interlocked.Increment(ref _ic_miss_count);
            return -1;
        }

        var typeKey = compute_ic_type_key(receiver);
        var funcIndex = slot.lookup(typeKey);

        if (funcIndex >= 0)
        {
            Interlocked.Increment(ref _ic_hit_count);
            _metrics?.record_ic_hit();
        }
        else
        {
            Interlocked.Increment(ref _ic_miss_count);
            _metrics?.record_ic_miss();
        }

        return funcIndex;
    }

    /// <summary>
    ///     更新内联缓存
    /// </summary>
    public void update_inline_cache(int cacheSlot, Value receiver, int functionIndex)
    {
        if (!_ic_slots.ContainsKey(cacheSlot)) _ic_slots[cacheSlot] = new InlineCacheSlot();

        var typeKey = compute_ic_type_key(receiver);
        _ic_slots[cacheSlot].update(typeKey, functionIndex);
    }

    /// <summary>
    ///     记录调用点命中，返回去虚拟化决策
    /// </summary>
    public DevirtualizationDecision record_call_site_hit(int cacheSlot, Value receiver, int functionIndex)
    {
        if (!_call_site_profiles.ContainsKey(cacheSlot)) _call_site_profiles[cacheSlot] = new CallSiteProfile();

        var profile = _call_site_profiles[cacheSlot];
        profile.total_calls++;
        var typeKey = compute_ic_type_key(receiver);
        var typeTag = typeKey.value_type_tag;

        profile.type_counts.TryAdd(typeTag, 0);

        profile.type_counts[typeTag]++;

        if (profile.total_calls < 100) return DevirtualizationDecision.no_action;

        var distinctTypes = profile.type_counts.Count;

        if (distinctTypes == 1)
        {
            profile.is_devirtualized = true;
            return DevirtualizationDecision.promote_to_static;
        }

        if (distinctTypes is >= 2 and <= 3)
        {
            profile.is_devirtualized = true;
            return DevirtualizationDecision.promote_to_witness;
        }

        return DevirtualizationDecision.keep_dynamic;
    }

    /// <summary>
    ///     根据去虚拟化决策修补代码字节流。
    /// </summary>
    public bool patch_bytecode(int cacheSlot, DevirtualizationDecision decision, int targetIndex)
    {
        if (_patchable_code_bytes == null) return false;

        if (!_cache_slot_pc_map.TryGetValue(cacheSlot, out var pc)) return false;

        if (decision is not (DevirtualizationDecision.promote_to_static or DevirtualizationDecision.promote_to_witness))
            return false;

        if (!_original_code_bytes_backup.ContainsKey(cacheSlot))
        {
            var backup = new byte[13];
            Array.Copy(_patchable_code_bytes, pc, backup, 0, Math.Min(13, _patchable_code_bytes.Length - pc));
            _original_code_bytes_backup[cacheSlot] = backup;
        }

        var opcode = decision == DevirtualizationDecision.promote_to_static
            ? (byte)NyarHeadCode.call_static
            : (byte)NyarHeadCode.call_witness;

        _patchable_code_bytes[pc] = opcode;

        _patchable_code_bytes[pc + 1] = (byte)(targetIndex & 0xFF);
        _patchable_code_bytes[pc + 2] = (byte)((targetIndex >> 8) & 0xFF);
        _patchable_code_bytes[pc + 3] = (byte)((targetIndex >> 16) & 0xFF);
        _patchable_code_bytes[pc + 4] = (byte)((targetIndex >> 24) & 0xFF);

        for (var i = 5; i < 13; i++)
            if (pc + i < _patchable_code_bytes.Length)
                _patchable_code_bytes[pc + i] = (byte)NyarHeadCode.nop;

        return true;
    }

    /// <summary>
    ///     获取调用点分析档案
    /// </summary>
    public CallSiteProfile? get_call_site_profile(int cacheSlot)
    {
        return _call_site_profiles.GetValueOrDefault(cacheSlot);
    }

    /// <summary>
    ///     失效指定缓存槽的内联缓存
    /// </summary>
    public bool invalidate_inline_cache(int cacheSlot)
    {
        var hadCache = _ic_slots.ContainsKey(cacheSlot);
        var hadProfile = _call_site_profiles.ContainsKey(cacheSlot);

        _ic_slots.Remove(cacheSlot);

        _call_site_profiles.Remove(cacheSlot);

        _inline_cache_versions.TryGetValue(cacheSlot, out var version);
        _inline_cache_versions[cacheSlot] = version + 1;

        revert_devirtualization(cacheSlot);

        return hadCache || hadProfile;
    }

    /// <summary>
    ///     失效指定缓存槽中特定类型的缓存条目
    /// </summary>
    public bool invalidate_inline_cache_type(int cacheSlot, byte typeTag)
    {
        if (!_ic_slots.TryGetValue(cacheSlot, out var slot)) return false;

        var typeKey = new IcTypeKey(typeTag, 0);
        var removed = slot.remove(typeKey);

        if (slot.is_empty)
        {
            _ic_slots.Remove(cacheSlot);

            if (_call_site_profiles.TryGetValue(cacheSlot, out var profile)) profile.type_counts.Remove(typeTag);

            _inline_cache_versions.TryGetValue(cacheSlot, out var version);
            _inline_cache_versions[cacheSlot] = version + 1;

            revert_devirtualization(cacheSlot);
        }
        else if (_call_site_profiles.TryGetValue(cacheSlot, out var profile))
        {
            profile.type_counts.Remove(typeTag);
        }

        return removed;
    }

    /// <summary>
    ///     清除所有内联缓存
    /// </summary>
    public void clear_inline_caches()
    {
        _ic_slots.Clear();
        _call_site_profiles.Clear();
        _inline_cache_versions.Clear();
        Interlocked.Exchange(ref _ic_hit_count, 0);
        Interlocked.Exchange(ref _ic_miss_count, 0);
    }

    #endregion

    #region 内部方法

    private bool try_compile(int funcIndex)
    {
        if (_code_bytes == null || _module == null) return false;

        if (_compiled_functions.ContainsKey(funcIndex)) return false;

        if (_compiled_functions.Count >= options.max_compiled_functions) return false;

        try
        {
            compile_function(funcIndex);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void compile_function(int funcIndex)
    {
        if (_code_bytes == null || _module == null) return;

        var callCount = _call_counts.GetValueOrDefault(funcIndex, 0);
        var func = _module.functions[funcIndex];
        var funcName = func.name ?? $"func_{funcIndex}";

        _events?.OnJitCompilationStarted(funcIndex, funcName);

        var sw = Stopwatch.StartNew();

        var compiledDelegate = _emit_compiler.compile(funcIndex, _code_bytes, _module);
        if (compiledDelegate == null) return;

        var intIntDelegate = _emit_compiler.compile_int_int(funcIndex, _code_bytes, _module);
        var doubleDoubleDelegate = _emit_compiler.compile_double_double(funcIndex, _code_bytes, _module);

        sw.Stop();

        var compiledFunc = new JitCompiledFunction(
            funcName,
            funcIndex,
            compiledDelegate,
            intIntDelegate,
            doubleDoubleDelegate,
            sw.Elapsed,
            callCount);

        _compiled_functions[funcIndex] = compiledFunc;
        total_compilations++;

        _metrics?.record_jit_compilation(sw.Elapsed.TotalMilliseconds);
        _events?.OnJitCompilationCompleted(funcIndex, funcName, sw.Elapsed.TotalMilliseconds);

        if (_cache != null && _module.name != null)
            try
            {
                _cache.record_compilation(_module.name, funcIndex);
            }
            catch
            {
            }
    }

    private void revert_devirtualization(int cacheSlot)
    {
        if (_patchable_code_bytes == null) return;

        if (!_original_code_bytes_backup.TryGetValue(cacheSlot, out var backup)) return;

        if (!_cache_slot_pc_map.TryGetValue(cacheSlot, out var pc)) return;

        var copyLen = Math.Min(backup.Length, _patchable_code_bytes.Length - pc);
        Array.Copy(backup, 0, _patchable_code_bytes, pc, copyLen);
        _original_code_bytes_backup.Remove(cacheSlot);
    }

    /// <summary>
    ///     计算 IC 类型键：组合 ValueType 和对象运行时类型哈希
    ///     对于 Object 类型的值，使用对象字典的键集合哈希作为类型区分
    /// </summary>
    private static IcTypeKey compute_ic_type_key(Value receiver)
    {
        var typeTag = (byte)receiver.type;

        if (receiver is { type: ValueType.@object, @object: Dictionary<string, Value> dict })
        {
            var hash = 0;
            foreach (var key in dict.Keys) hash = HashCode.Combine(hash, key);

            return new IcTypeKey(typeTag, hash);
        }

        return new IcTypeKey(typeTag, 0);
    }

    #endregion
}
