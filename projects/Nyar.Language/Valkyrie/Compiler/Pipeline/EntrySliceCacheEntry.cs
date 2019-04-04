namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     Entry 切片缓存条目，按 "target + entry + callgraph hash" 缓存。
/// </summary>
public sealed record EntrySliceCacheEntry
{
    /// <summary>
    ///     入口函数名
    /// </summary>
    public string entry_name { get; init; } = string.Empty;

    /// <summary>
    ///     可达函数名列表
    /// </summary>
    public IReadOnlyList<string> reachable_functions { get; init; } = [];

    /// <summary>
    ///     调用图哈希
    /// </summary>
    public string call_graph_hash { get; init; } = string.Empty;

    /// <summary>
    ///     目标 CanonicalTriple
    /// </summary>
    public string canonical_triple { get; init; } = string.Empty;

    /// <summary>
    ///     缓存创建时间
    /// </summary>
    public DateTimeOffset created_at { get; init; }
}