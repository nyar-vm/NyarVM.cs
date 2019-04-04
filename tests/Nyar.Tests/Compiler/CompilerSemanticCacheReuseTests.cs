using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Nyar.Tests.Compiler;

public sealed class CompilerSemanticCacheReuseTests : IncrementalCacheTestBase
{
    [Fact]
    public void CheckFiles_SecondRun_ShouldHitSemanticModelCache()
    {
        var sourcePath = context.create_semantic_cache_source();
        var plan = new BuildPlan("simple_cli", "jvm-openjdk-linux-managed", sourcePath);
        var cache = new TrackingCompilationCache(context.create_cache());

        var firstCompiler = new ValkyrieCompiler(cache);
        var firstSemantics = firstCompiler.check_files([sourcePath], plan);
        var firstStats = cache.snapshot_semantic_stats();

        IncrementalCacheTestAssertions.assert_semantic_model_ok(firstSemantics);
        IncrementalCacheTestAssertions.assert_semantic_cache_populated(firstStats);

        var secondCompiler = new ValkyrieCompiler(cache);
        var secondSemantics = secondCompiler.check_files([sourcePath], plan);
        var secondStats = cache.snapshot_semantic_stats();

        IncrementalCacheTestAssertions.assert_semantic_model_ok(secondSemantics);
        IncrementalCacheTestAssertions.assert_semantic_cache_reused(firstStats, secondStats);
    }
}
