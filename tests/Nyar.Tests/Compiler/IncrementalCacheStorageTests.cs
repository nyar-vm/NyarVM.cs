using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Nyar.Tests.Compiler;

public sealed class IncrementalCacheStorageTests : IncrementalCacheTestBase
{
    [Fact]
    public void Cache_Creation_CreatesDotCacheDirectory()
    {
        var cache = create_cache();
        Assert.Equal(Path.Combine(context.workspace_dir, ".cache"), cache.cache_root);
    }

    [Fact]
    public void IrCache_RoundTrip_PutThenGet_ReturnsSameData()
    {
        var cache = create_cache();
        var irData = new byte[] { 1, 2, 3, 4, 5 };

        IncrementalCacheTestAssertions.put_ir(cache, "test_module", DefaultTriple, "abc123", irData);
        IncrementalCacheTestAssertions.assert_ir_hit(cache, "test_module", DefaultTriple, "abc123", irData);
    }

    [Fact]
    public void IrCache_Miss_ReturnsFalse()
    {
        var cache = create_cache();
        IncrementalCacheTestAssertions.assert_ir_miss(cache, "nonexistent", DefaultTriple, "no_hash");
    }

    [Fact]
    public void IrCache_DifferentTriple_DifferentBuckets()
    {
        var cache = create_cache();
        var irData = new byte[] { 42 };

        IncrementalCacheTestAssertions.put_ir(cache, "m", DefaultTriple, "h1", irData);
        IncrementalCacheTestAssertions.assert_ir_miss(cache, "m", WasmTriple, "h1");
    }

    [Fact]
    public void IrCache_DifferentHash_DifferentEntry()
    {
        var cache = create_cache();

        IncrementalCacheTestAssertions.put_ir(cache, "m", DefaultTriple, "h1", [1]);
        IncrementalCacheTestAssertions.put_ir(cache, "m", DefaultTriple, "h2", [2], "HIR");

        IncrementalCacheTestAssertions.assert_ir_hit(cache, "m", DefaultTriple, "h1", [1]);
        IncrementalCacheTestAssertions.assert_ir_hit(cache, "m", DefaultTriple, "h2", [2], "HIR");
    }

    [Fact]
    public void IrCache_Invalidate_ClearsSpecifiedTriple()
    {
        var cache = create_cache();

        IncrementalCacheTestAssertions.put_ir(cache, "m", DefaultTriple, "h1", [1]);
        IncrementalCacheTestAssertions.put_ir(cache, "m", WasmTriple, "h2", [2], "HIR");

        cache.invalidate(DefaultTriple);

        IncrementalCacheTestAssertions.assert_ir_miss(cache, "m", DefaultTriple, "h1");
        IncrementalCacheTestAssertions.assert_ir_hit(cache, "m", WasmTriple, "h2", [2], "HIR");
    }

    [Fact]
    public void IrCache_InvalidateAll_ClearsAll()
    {
        var cache = create_cache();

        IncrementalCacheTestAssertions.put_ir(cache, "m", DefaultTriple, "h1", [1]);
        IncrementalCacheTestAssertions.put_ir(cache, "m", WasmTriple, "h2", [2], "HIR");

        cache.invalidate_all();

        IncrementalCacheTestAssertions.assert_ir_miss(cache, "m", DefaultTriple, "h1");
        IncrementalCacheTestAssertions.assert_ir_miss(cache, "m", WasmTriple, "h2");
        Assert.False(Directory.Exists(Path.Combine(context.workspace_dir, ".cache")));
    }

    [Fact]
    public void TokenCache_RoundTrip_PutThenGet()
    {
        var cache = create_cache();
        var tokenData = new byte[] { 10, 20, 30 };

        IncrementalCacheTestAssertions.put_tokens(cache, "test.v", "file_hash_001", tokenData);
        IncrementalCacheTestAssertions.assert_token_hit(cache, "test.v", "file_hash_001", tokenData);
    }

    [Fact]
    public void TokenCache_DifferentHash_Miss()
    {
        var cache = create_cache();

        IncrementalCacheTestAssertions.put_tokens(cache, "test.v", "file_hash_001", [1]);
        IncrementalCacheTestAssertions.assert_token_miss(cache, "test.v", "file_hash_002");
    }

    [Fact]
    public void StagingCache_RoundTrip_PutThenGet()
    {
        var cache = create_cache();
        var stagedData = new byte[] { 100, 200 };

        IncrementalCacheTestAssertions.put_staging(cache, "test.v", DefaultTriple, "content_hash_x", stagedData);
        IncrementalCacheTestAssertions.assert_staging_hit(cache, "test.v", DefaultTriple, "content_hash_x", stagedData);
    }

    [Fact]
    public void SemanticCache_RoundTrip_PutThenGet()
    {
        var cache = create_cache();
        var semanticData = new byte[] { 7, 8, 9 };

        IncrementalCacheTestAssertions.put_semantics(cache, "test.v", DefaultTriple, "ast_hash_xyz", semanticData);
        IncrementalCacheTestAssertions.assert_semantic_hit(cache, "test.v", DefaultTriple, "ast_hash_xyz", semanticData);
    }

    [Fact]
    public void EntrySliceCache_RoundTrip_PutThenGet()
    {
        var cache = create_cache();
        var reachableFunctions = new[] { "main", "helper", "init" };

        IncrementalCacheTestAssertions.put_entry_slice(cache, "main", DefaultTriple, "call_graph_hash", reachableFunctions);
        IncrementalCacheTestAssertions.assert_entry_slice_hit(cache, "main", DefaultTriple, "call_graph_hash", reachableFunctions);
    }

    [Fact]
    public void Cache_Persistence_AcrossInstances()
    {
        var triple = "jvm-openjdk-unknown-managed";
        var irData = new byte[] { 99, 100, 101 };

        var cache1 = create_cache();
        IncrementalCacheTestAssertions.put_ir(cache1, "persist_module", triple, "persist_hash", irData);

        var cache2 = create_cache();
        IncrementalCacheTestAssertions.assert_ir_hit(cache2, "persist_module", triple, "persist_hash", irData);
    }

    [Fact]
    public void Cache_BucketDirectory_ContainsTripleName()
    {
        var cache = create_cache();
        IncrementalCacheTestAssertions.put_ir(cache, "m", WasmTriple, "h", [1]);

        var cacheRoot = Path.Combine(context.workspace_dir, ".cache");
        var bucketDir = Path.Combine(cacheRoot, "wasm32_unknown_browser_wasm");
        Assert.True(Directory.Exists(bucketDir));

        var files = Directory.GetFiles(bucketDir, "*.nyar");
        Assert.Single(files);
    }

    [Fact]
    public void ComputeFileHash_SameContent_SameHash()
    {
        var testFile = context.write_source_file("test_source.v", "micro add(x: i32, y: i32) -> i32 { x + y }");

        var hash1 = NyarDatabaseCompilationCache.compute_file_hash(testFile);
        var hash2 = NyarDatabaseCompilationCache.compute_file_hash(testFile);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeFileHash_DifferentContent_DifferentHash()
    {
        var file1 = context.write_source_file("file1.v", "micro a() { 1 }");
        var file2 = context.write_source_file("file2.v", "micro a() { 2 }");

        var hash1 = NyarDatabaseCompilationCache.compute_file_hash(file1);
        var hash2 = NyarDatabaseCompilationCache.compute_file_hash(file2);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeFilesHash_OrderIndependent_SameHash()
    {
        var file1 = context.write_source_file("a.v", "micro f() { 1 }");
        var file2 = context.write_source_file("b.v", "micro g() { 2 }");

        var hashForward = NyarDatabaseCompilationCache.compute_files_hash([file1, file2]);
        var hashReverse = NyarDatabaseCompilationCache.compute_files_hash([file2, file1]);

        Assert.Equal(hashForward, hashReverse);
    }

    [Fact]
    public void ComputeFilesHash_DifferentSet_DifferentHash()
    {
        var file1 = context.write_source_file("x.v", "micro x() { 1 }");
        var file2 = context.write_source_file("y.v", "micro y() { 2 }");
        var file3 = context.write_source_file("z.v", "micro z() { 3 }");

        var hash12 = NyarDatabaseCompilationCache.compute_files_hash([file1, file2]);
        var hash123 = NyarDatabaseCompilationCache.compute_files_hash([file1, file2, file3]);

        Assert.NotEqual(hash12, hash123);
    }
}
