using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Nyar.Tests.Compiler;

internal readonly record struct SemanticCacheStats(int get_count, int hit_count, int put_count);

/// <summary>
///     为缓存测试提供语义缓存命中统计，同时透传其它缓存行为。
/// </summary>
internal sealed class TrackingCompilationCache : ICompilationCache
{
    private readonly ICompilationCache _inner;

    public TrackingCompilationCache(ICompilationCache inner)
    {
        _inner = inner;
    }

    public int semantic_get_count { get; private set; }
    public int semantic_hit_count { get; private set; }
    public int semantic_put_count { get; private set; }

    public SemanticCacheStats snapshot_semantic_stats()
    {
        return new SemanticCacheStats(semantic_get_count, semantic_hit_count, semantic_put_count);
    }

    public bool try_get_tokens(string filePath, string contentHash, out TokenCacheEntry? tokens) => _inner.try_get_tokens(filePath, contentHash, out tokens);

    public void put_tokens(string filePath, string contentHash, TokenCacheEntry entry) => _inner.put_tokens(filePath, contentHash, entry);

    public bool try_get_staging(string filePath, string canonicalTriple, string contentHash, out StageCacheEntry? stagedTokens) => _inner.try_get_staging(filePath, canonicalTriple, contentHash, out stagedTokens);

    public void put_staging(string filePath, string canonicalTriple, string contentHash, StageCacheEntry entry) => _inner.put_staging(filePath, canonicalTriple, contentHash, entry);

    public bool try_get_semantics(string filePath, string canonicalTriple, string astHash, out SemanticCacheEntry? semantics)
    {
        semantic_get_count++;
        var found = _inner.try_get_semantics(filePath, canonicalTriple, astHash, out semantics);
        if (found)
        {
            semantic_hit_count++;
        }

        return found;
    }

    public void put_semantics(string filePath, string canonicalTriple, string astHash, SemanticCacheEntry entry)
    {
        semantic_put_count++;
        _inner.put_semantics(filePath, canonicalTriple, astHash, entry);
    }

    public bool try_get_ir(string moduleName, string canonicalTriple, string irHash, out IrCacheEntry? irEntry) => _inner.try_get_ir(moduleName, canonicalTriple, irHash, out irEntry);

    public void put_ir(string moduleName, string canonicalTriple, string irHash, IrCacheEntry entry) => _inner.put_ir(moduleName, canonicalTriple, irHash, entry);

    public bool try_get_entry_slice(string entryName, string canonicalTriple, string callGraphHash, out EntrySliceCacheEntry? entrySlice) => _inner.try_get_entry_slice(entryName, canonicalTriple, callGraphHash, out entrySlice);

    public void put_entry_slice(string entryName, string canonicalTriple, string callGraphHash, EntrySliceCacheEntry entry) => _inner.put_entry_slice(entryName, canonicalTriple, callGraphHash, entry);

    public void invalidate(string canonicalTriple) => _inner.invalidate(canonicalTriple);

    public void invalidate_all() => _inner.invalidate_all();
}
