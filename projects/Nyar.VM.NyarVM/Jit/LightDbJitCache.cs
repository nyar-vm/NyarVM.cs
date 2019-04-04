using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Core.Database;

namespace Nyar.VM.NyarVM.Jit;

/// <summary>
///     基于 LightDB 缓存的 JIT 编译结果持久化实现
///     使用 IDatabaseCache 存储热点函数信息，避免 VM 重启后重复识别热点
///     每个模块的热点函数列表以单个缓存条目存储，键为 "jit:{moduleName}"
/// </summary>
public sealed class LightDbJitCache : IJitCache
{
    private readonly IDatabaseCache _cache;
    private readonly ConcurrentDictionary<string, HashSet<int>> _in_memory_index = new();

    /// <summary>
    ///     创建 LightDB JIT 缓存
    /// </summary>
    /// <param name="cache">数据库缓存实例。</param>
    public LightDbJitCache(IDatabaseCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public void record_compilation(string moduleName, int functionIndex)
    {
        var set = _in_memory_index.GetOrAdd(moduleName, _ => []);
        bool changed;

        lock (set)
        {
            changed = set.Add(functionIndex);
        }

        if (changed) flush_module(moduleName);
    }

    /// <inheritdoc />
    public bool was_compiled_previously(string moduleName, int functionIndex)
    {
        var set = _in_memory_index.GetOrAdd(moduleName, _ => []);

        lock (set)
        {
            if (set.Contains(functionIndex)) return true;
        }

        load_module_if_needed(moduleName);

        lock (set)
        {
            return set.Contains(functionIndex);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<int> get_previously_compiled_functions(string moduleName)
    {
        load_module_if_needed(moduleName);

        var set = _in_memory_index.GetOrAdd(moduleName, _ => []);

        lock (set)
        {
            return [.. set];
        }
    }

    /// <inheritdoc />
    public void invalidate(string moduleName)
    {
        _in_memory_index.TryRemove(moduleName, out _);
        var key = build_module_key(moduleName);
        _cache.RemoveAsync(key).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void invalidate(string moduleName, int functionIndex)
    {
        var set = _in_memory_index.GetOrAdd(moduleName, _ => []);
        bool changed;

        lock (set)
        {
            changed = set.Remove(functionIndex);
        }

        if (changed) flush_module(moduleName);
    }

    /// <summary>
    ///     从缓存加载模块的热点函数列表到内存索引
    /// </summary>
    private void load_module_if_needed(string moduleName)
    {
        if (_in_memory_index.ContainsKey(moduleName)) return;

        var key = build_module_key(moduleName);
        var raw = _cache.GetAsync(key).GetAwaiter().GetResult();

        var set = _in_memory_index.GetOrAdd(moduleName, _ => []);

        if (raw is { Length: > 0 })
        {
            var entry = JsonSerializer.Deserialize<JitCacheEntry>(raw.Value.Span);
            if (entry?.function_indices is not null)
                lock (set)
                {
                    foreach (var idx in entry.function_indices) set.Add(idx);
                }
        }
    }

    /// <summary>
    ///     将内存索引中的热点函数列表刷新到缓存
    /// </summary>
    private void flush_module(string moduleName)
    {
        var set = _in_memory_index.GetOrAdd(moduleName, _ => []);
        List<int> snapshot;

        lock (set)
        {
            snapshot = [.. set];
        }

        var key = build_module_key(moduleName);
        var entry = new JitCacheEntry
        {
            module_name = moduleName,
            function_indices = snapshot
        };

        var serialized = JsonSerializer.SerializeToUtf8Bytes(entry);
        _cache.PutAsync(key, serialized).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     构建模块级缓存键：jit:{moduleName}
    /// </summary>
    private static ReadOnlyMemory<byte> build_module_key(string moduleName)
    {
        var raw = $"jit:{moduleName}";
        return Encoding.UTF8.GetBytes(raw);
    }

    /// <summary>
    ///     JIT 缓存条目，存储单个模块的热点函数列表
    /// </summary>
    private sealed class JitCacheEntry
    {
        /// <summary>
        ///     模块名称
        /// </summary>
        public string module_name { get; set; } = "";


        /// <summary>
        ///     历史上被 JIT 编译过的函数索引列表
        /// </summary>
        public List<int> function_indices { get; set; } = [];
    }
}