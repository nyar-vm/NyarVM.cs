namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     编译缓存接口，为增量编译提供各阶段缓存查询与写入能力。
///     缓存键以 "target-specialized module" 为粒度，而非仅文件级。
/// </summary>
public interface ICompilationCache
{
    /// <summary>
    ///     尝试获取缓存的 tokenstream。
    /// </summary>
    /// <param name="filePath">源文件路径</param>
    /// <param name="contentHash">源文件内容哈希</param>
    /// <param name="tokens">命中时返回的 tokenstream</param>
    /// <returns>是否命中</returns>
    bool try_get_tokens(string filePath, string contentHash, out TokenCacheEntry? tokens);

    /// <summary>
    ///     写入 tokenstream 缓存。
    /// </summary>
    void put_tokens(string filePath, string contentHash, TokenCacheEntry entry);

    /// <summary>
    ///     尝试获取缓存的 staging 结果。
    /// </summary>
    /// <param name="filePath">源文件路径</param>
    /// <param name="canonicalTriple">目标 CanonicalTriple</param>
    /// <param name="contentHash">源文件内容哈希</param>
    /// <param name="stagedTokens">命中时返回的特化 tokenstream</param>
    /// <returns>是否命中</returns>
    bool try_get_staging(string filePath, string canonicalTriple, string contentHash,
        out StageCacheEntry? stagedTokens);

    /// <summary>
    ///     写入 staging 缓存。
    /// </summary>
    void put_staging(string filePath, string canonicalTriple, string contentHash, StageCacheEntry entry);

    /// <summary>
    ///     尝试获取缓存的语义模型。
    /// </summary>
    /// <param name="filePath">源文件路径</param>
    /// <param name="canonicalTriple">目标 CanonicalTriple</param>
    /// <param name="astHash">已特化 AST 的哈希</param>
    /// <param name="semantics">命中时返回的语义模型</param>
    /// <returns>是否命中</returns>
    bool try_get_semantics(string filePath, string canonicalTriple, string astHash,
        out SemanticCacheEntry? semantics);

    /// <summary>
    ///     写入语义模型缓存。
    /// </summary>
    void put_semantics(string filePath, string canonicalTriple, string astHash, SemanticCacheEntry entry);

    /// <summary>
    ///     尝试获取缓存的 IR（HIR/MIR/LIR）。
    /// </summary>
    /// <param name="moduleName">模块名</param>
    /// <param name="canonicalTriple">目标 CanonicalTriple</param>
    /// <param name="irHash">IR 哈希</param>
    /// <param name="irEntry">命中时返回的 IR 缓存条目</param>
    /// <returns>是否命中</returns>
    bool try_get_ir(string moduleName, string canonicalTriple, string irHash,
        out IrCacheEntry? irEntry);

    /// <summary>
    ///     写入 IR 缓存。
    /// </summary>
    void put_ir(string moduleName, string canonicalTriple, string irHash, IrCacheEntry entry);

    /// <summary>
    ///     尝试获取缓存的 entry 切片结果。
    /// </summary>
    /// <param name="entryName">入口函数名</param>
    /// <param name="canonicalTriple">目标 CanonicalTriple</param>
    /// <param name="callGraphHash">调用图哈希</param>
    /// <param name="entrySlice">命中时返回的切片结果</param>
    /// <returns>是否命中</returns>
    bool try_get_entry_slice(string entryName, string canonicalTriple, string callGraphHash,
        out EntrySliceCacheEntry? entrySlice);

    /// <summary>
    ///     写入 entry 切片缓存。
    /// </summary>
    void put_entry_slice(string entryName, string canonicalTriple, string callGraphHash,
        EntrySliceCacheEntry entry);

    /// <summary>
    ///     清除指定 CanonicalTriple 的全部缓存。
    /// </summary>
    void invalidate(string canonicalTriple);

    /// <summary>
    ///     清除所有缓存。
    /// </summary>
    void invalidate_all();
}