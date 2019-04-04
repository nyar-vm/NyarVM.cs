using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Nyar.Tests.Compiler;

/// <summary>
///     聚合增量缓存测试中复用的缓存写入与断言逻辑。
/// </summary>
internal static class IncrementalCacheTestAssertions
{
    public static void put_ir(
        NyarDatabaseCompilationCache cache,
        string moduleName,
        string canonicalTriple,
        string hash,
        byte[] irData,
        string irKind = "LIR")
    {
        cache.put_ir(moduleName, canonicalTriple, hash, new IrCacheEntry
        {
            ir_kind = irKind,
            ir_hash = hash,
            canonical_triple = canonicalTriple,
            ir_data = irData,
            created_at = DateTimeOffset.UtcNow
        });
    }

    public static void put_tokens(
        NyarDatabaseCompilationCache cache,
        string filePath,
        string contentHash,
        byte[] tokenData)
    {
        cache.put_tokens(filePath, contentHash, new TokenCacheEntry
        {
            content_hash = contentHash,
            token_data = tokenData,
            created_at = DateTimeOffset.UtcNow
        });
    }

    public static void put_staging(
        NyarDatabaseCompilationCache cache,
        string filePath,
        string canonicalTriple,
        string contentHash,
        byte[] stagedTokenData)
    {
        cache.put_staging(filePath, canonicalTriple, contentHash, new StageCacheEntry
        {
            content_hash = contentHash,
            canonical_triple = canonicalTriple,
            staged_token_data = stagedTokenData,
            created_at = DateTimeOffset.UtcNow
        });
    }

    public static void put_semantics(
        NyarDatabaseCompilationCache cache,
        string filePath,
        string canonicalTriple,
        string astHash,
        byte[] semanticData)
    {
        cache.put_semantics(filePath, canonicalTriple, astHash, new SemanticCacheEntry
        {
            ast_hash = astHash,
            canonical_triple = canonicalTriple,
            semantic_data = semanticData,
            created_at = DateTimeOffset.UtcNow
        });
    }

    public static void put_entry_slice(
        NyarDatabaseCompilationCache cache,
        string entryName,
        string canonicalTriple,
        string callGraphHash,
        string[] reachableFunctions)
    {
        cache.put_entry_slice(entryName, canonicalTriple, callGraphHash, new EntrySliceCacheEntry
        {
            entry_name = entryName,
            canonical_triple = canonicalTriple,
            call_graph_hash = callGraphHash,
            reachable_functions = reachableFunctions,
            created_at = DateTimeOffset.UtcNow
        });
    }

    public static void assert_ir_hit(
        NyarDatabaseCompilationCache cache,
        string moduleName,
        string canonicalTriple,
        string hash,
        byte[] expectedData,
        string expectedKind = "LIR")
    {
        var found = cache.try_get_ir(moduleName, canonicalTriple, hash, out var retrieved);
        Assert.True(found);
        Assert.NotNull(retrieved);
        Assert.Equal(hash, retrieved!.ir_hash);
        Assert.Equal(expectedKind, retrieved.ir_kind);
        Assert.Equal(canonicalTriple, retrieved.canonical_triple);
        Assert.Equal(expectedData, retrieved.ir_data);
    }

    public static void assert_ir_miss(
        NyarDatabaseCompilationCache cache,
        string moduleName,
        string canonicalTriple,
        string hash)
    {
        var found = cache.try_get_ir(moduleName, canonicalTriple, hash, out var retrieved);
        Assert.False(found);
        Assert.Null(retrieved);
    }

    public static void assert_token_hit(
        NyarDatabaseCompilationCache cache,
        string filePath,
        string contentHash,
        byte[] expectedData)
    {
        var found = cache.try_get_tokens(filePath, contentHash, out var retrieved);
        Assert.True(found);
        Assert.NotNull(retrieved);
        Assert.Equal(contentHash, retrieved!.content_hash);
        Assert.Equal(expectedData, retrieved.token_data);
    }

    public static void assert_token_miss(
        NyarDatabaseCompilationCache cache,
        string filePath,
        string contentHash)
    {
        var found = cache.try_get_tokens(filePath, contentHash, out _);
        Assert.False(found);
    }

    public static void assert_staging_hit(
        NyarDatabaseCompilationCache cache,
        string filePath,
        string canonicalTriple,
        string contentHash,
        byte[] expectedData)
    {
        var found = cache.try_get_staging(filePath, canonicalTriple, contentHash, out var retrieved);
        Assert.True(found);
        Assert.NotNull(retrieved);
        Assert.Equal(expectedData, retrieved!.staged_token_data);
    }

    public static void assert_semantic_hit(
        NyarDatabaseCompilationCache cache,
        string filePath,
        string canonicalTriple,
        string astHash,
        byte[] expectedData)
    {
        var found = cache.try_get_semantics(filePath, canonicalTriple, astHash, out var retrieved);
        Assert.True(found);
        Assert.NotNull(retrieved);
        Assert.Equal(expectedData, retrieved!.semantic_data);
    }

    public static void assert_entry_slice_hit(
        NyarDatabaseCompilationCache cache,
        string entryName,
        string canonicalTriple,
        string callGraphHash,
        string[] reachableFunctions)
    {
        var found = cache.try_get_entry_slice(entryName, canonicalTriple, callGraphHash, out var retrieved);
        Assert.True(found);
        Assert.NotNull(retrieved);
        Assert.Equal(entryName, retrieved!.entry_name);
        Assert.Equal(reachableFunctions, retrieved.reachable_functions);
    }

    public static void assert_semantic_model_ok(SemanticModel semantics)
    {
        Assert.False(semantics.has_errors);
        assert_function_signature(semantics, "simple_cli::main", "i32");
        assert_function_signature(semantics, "simple_cli::helper", "i32");
        Assert.Contains(semantics.get_all_declared_symbols(), symbol => symbol.name == "main");
        Assert.Contains(semantics.get_all_declared_symbols(), symbol => symbol.name == "helper");
    }

    public static void assert_function_signature(
        SemanticModel semantics,
        string qualifiedName,
        string expectedReturnType,
        int expectedParameterCount = 0)
    {
        var symbol = Assert.IsType<Symbol>(semantics.find_symbol_by_name(qualifiedName));
        var functionType = Assert.IsType<FunctionType>(symbol.type);
        var returnType = Assert.IsAssignableFrom<IType>(functionType.return_type);

        Assert.Equal(expectedParameterCount, functionType.parameter_types.Count);
        Assert.Equal(expectedReturnType, returnType.name);
    }

    public static void assert_semantic_cache_populated(SemanticCacheStats stats)
    {
        Assert.True(stats.get_count >= 1);
        Assert.Equal(0, stats.hit_count);
        Assert.Equal(1, stats.put_count);
    }

    public static void assert_semantic_cache_reused(SemanticCacheStats baseline, SemanticCacheStats current)
    {
        Assert.Equal(baseline.put_count, current.put_count);
        Assert.True(current.get_count > baseline.get_count);
        Assert.True(current.hit_count > baseline.hit_count);
    }
}
