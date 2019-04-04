namespace Nyar.Tests.Jit;

public class LightDbJitCacheTests
{
    /// <summary>
    ///     创建测试用的 LightCache 实例（基于临时目录的 LightDatabase）
    /// </summary>
    private static (ILightCache Cache, string TempDir) create_test_cache()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_jit_test_{Guid.NewGuid():N}");
        var options = new LightOptions
        {
            Path = tempDir
        };
        var db = new LightDatabase(options);
        var cache = new LightCache(db);
        return (cache, tempDir);
    }

    /// <summary>
    ///     清理临时目录
    /// </summary>
    private static void cleanup(string tempDir)
    {
        try
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
        catch
        {
            // 忽略清理失败
        }
    }

    /// <summary>
    ///     测试 RecordCompilation 和 WasCompiledPreviously
    /// </summary>
    [Fact]
    public void RecordCompilation_WasCompiledPreviously_ReturnsTrue()
    {
        var (cache, tempDir) = create_test_cache();
        try
        {
            var jitCache = new LightDbJitCache(cache);

            jitCache.RecordCompilation("test_module", 0);
            jitCache.RecordCompilation("test_module", 3);
            jitCache.RecordCompilation("test_module", 7);

            Assert.True(jitCache.WasCompiledPreviously("test_module", 0));
            Assert.True(jitCache.WasCompiledPreviously("test_module", 3));
            Assert.True(jitCache.WasCompiledPreviously("test_module", 7));
            Assert.False(jitCache.WasCompiledPreviously("test_module", 1));
            Assert.False(jitCache.WasCompiledPreviously("other_module", 0));
        }
        finally
        {
            cleanup(tempDir);
        }
    }

    /// <summary>
    ///     测试 GetPreviouslyCompiledFunctions
    /// </summary>
    [Fact]
    public void GetPreviouslyCompiledFunctions_ReturnsAllRecordedIndices()
    {
        var (cache, tempDir) = create_test_cache();
        try
        {
            var jitCache = new LightDbJitCache(cache);

            jitCache.RecordCompilation("test_module", 0);
            jitCache.RecordCompilation("test_module", 5);
            jitCache.RecordCompilation("test_module", 10);

            var functions = jitCache.GetPreviouslyCompiledFunctions("test_module");

            Assert.Equal(3, functions.Count);
            Assert.Contains(0, functions);
            Assert.Contains(5, functions);
            Assert.Contains(10, functions);
        }
        finally
        {
            cleanup(tempDir);
        }
    }

    /// <summary>
    ///     测试 GetPreviouslyCompiledFunctions 空模块
    /// </summary>
    [Fact]
    public void GetPreviouslyCompiledFunctions_EmptyModule_ReturnsEmptyList()
    {
        var (cache, tempDir) = create_test_cache();
        try
        {
            var jitCache = new LightDbJitCache(cache);

            var functions = jitCache.GetPreviouslyCompiledFunctions("nonexistent");

            Assert.Empty(functions);
        }
        finally
        {
            cleanup(tempDir);
        }
    }

    /// <summary>
    ///     测试 Invalidate 整个模块
    /// </summary>
    [Fact]
    public void Invalidate_Module_RemovesAllEntries()
    {
        var (cache, tempDir) = create_test_cache();
        try
        {
            var jitCache = new LightDbJitCache(cache);

            jitCache.RecordCompilation("test_module", 0);
            jitCache.RecordCompilation("test_module", 1);
            jitCache.RecordCompilation("test_module", 2);

            jitCache.Invalidate("test_module");

            Assert.False(jitCache.WasCompiledPreviously("test_module", 0));
            Assert.False(jitCache.WasCompiledPreviously("test_module", 1));
            Assert.False(jitCache.WasCompiledPreviously("test_module", 2));
            Assert.Empty(jitCache.GetPreviouslyCompiledFunctions("test_module"));
        }
        finally
        {
            cleanup(tempDir);
        }
    }

    /// <summary>
    ///     测试 Invalidate 单个函数
    /// </summary>
    [Fact]
    public void Invalidate_SingleFunction_RemovesOnlyThatEntry()
    {
        var (cache, tempDir) = create_test_cache();
        try
        {
            var jitCache = new LightDbJitCache(cache);

            jitCache.RecordCompilation("test_module", 0);
            jitCache.RecordCompilation("test_module", 1);
            jitCache.RecordCompilation("test_module", 2);

            jitCache.Invalidate("test_module", 1);

            Assert.True(jitCache.WasCompiledPreviously("test_module", 0));
            Assert.False(jitCache.WasCompiledPreviously("test_module", 1));
            Assert.True(jitCache.WasCompiledPreviously("test_module", 2));

            var functions = jitCache.GetPreviouslyCompiledFunctions("test_module");
            Assert.Equal(2, functions.Count);
            Assert.DoesNotContain(1, functions);
        }
        finally
        {
            cleanup(tempDir);
        }
    }

    /// <summary>
    ///     测试缓存持久化：创建新 LightDbJitCache 实例后仍能读取之前记录的数据
    /// </summary>
    [Fact]
    public void Persistence_NewCacheInstance_CanReadPreviousData()
    {
        var (lightCache, tempDir) = create_test_cache();
        try
        {
            var jitCache1 = new LightDbJitCache(lightCache);
            jitCache1.RecordCompilation("my_module", 0);
            jitCache1.RecordCompilation("my_module", 5);
            jitCache1.RecordCompilation("my_module", 10);

            var jitCache2 = new LightDbJitCache(lightCache);

            var functions = jitCache2.GetPreviouslyCompiledFunctions("my_module");
            Assert.Equal(3, functions.Count);
            Assert.Contains(0, functions);
            Assert.Contains(5, functions);
            Assert.Contains(10, functions);
        }
        finally
        {
            cleanup(tempDir);
        }
    }

    /// <summary>
    ///     测试多模块互不干扰
    /// </summary>
    [Fact]
    public void MultipleModules_DontInterfere()
    {
        var (cache, tempDir) = create_test_cache();
        try
        {
            var jitCache = new LightDbJitCache(cache);

            jitCache.RecordCompilation("module_a", 0);
            jitCache.RecordCompilation("module_a", 1);
            jitCache.RecordCompilation("module_b", 5);
            jitCache.RecordCompilation("module_b", 6);

            Assert.True(jitCache.WasCompiledPreviously("module_a", 0));
            Assert.False(jitCache.WasCompiledPreviously("module_a", 5));
            Assert.True(jitCache.WasCompiledPreviously("module_b", 5));
            Assert.False(jitCache.WasCompiledPreviously("module_b", 0));

            jitCache.Invalidate("module_a");

            Assert.False(jitCache.WasCompiledPreviously("module_a", 0));
            Assert.True(jitCache.WasCompiledPreviously("module_b", 5));
        }
        finally
        {
            cleanup(tempDir);
        }
    }

    /// <summary>
    ///     测试重复 RecordCompilation 不产生重复条目
    /// </summary>
    [Fact]
    public void RecordCompilation_DuplicateEntry_NoDuplicates()
    {
        var (cache, tempDir) = create_test_cache();
        try
        {
            var jitCache = new LightDbJitCache(cache);

            jitCache.RecordCompilation("test_module", 0);
            jitCache.RecordCompilation("test_module", 0);
            jitCache.RecordCompilation("test_module", 0);

            var functions = jitCache.GetPreviouslyCompiledFunctions("test_module");
            Assert.Single(functions);
            Assert.Contains(0, functions);
        }
        finally
        {
            cleanup(tempDir);
        }
    }

    /// <summary>
    ///     测试 Invalidate 不存在的模块不抛异常
    /// </summary>
    [Fact]
    public void Invalidate_NonexistentModule_NoException()
    {
        var (cache, tempDir) = create_test_cache();
        try
        {
            var jitCache = new LightDbJitCache(cache);

            var ex = Record.Exception(() => jitCache.Invalidate("nonexistent"));
            Assert.Null(ex);
        }
        finally
        {
            cleanup(tempDir);
        }
    }

    /// <summary>
    ///     测试 Invalidate 不存在的函数不抛异常
    /// </summary>
    [Fact]
    public void Invalidate_NonexistentFunction_NoException()
    {
        var (cache, tempDir) = create_test_cache();
        try
        {
            var jitCache = new LightDbJitCache(cache);

            var ex = Record.Exception(() => jitCache.Invalidate("nonexistent", 42));
            Assert.Null(ex);
        }
        finally
        {
            cleanup(tempDir);
        }
    }

    /// <summary>
    ///     测试 NyarVM.EnableJitCache 方法
    /// </summary>
    [Fact]
    public void NyarVM_EnableJitCache_SetsCacheOnJitCompiler()
    {
        var (cache, tempDir) = create_test_cache();
        try
        {
            var vm = new NyarVM();
            var jitCache = new LightDbJitCache(cache);
            vm.EnableJitCache(jitCache);

            Assert.NotNull(vm.JitCompiler);
        }
        finally
        {
            cleanup(tempDir);
        }
    }

    /// <summary>
    ///     测试大量函数索引的缓存
    /// </summary>
    [Fact]
    public void RecordCompilation_ManyFunctions_AllAccessible()
    {
        var (cache, tempDir) = create_test_cache();
        try
        {
            var jitCache = new LightDbJitCache(cache);

            for (var i = 0; i < 50; i++) jitCache.RecordCompilation("large_module", i);

            var functions = jitCache.GetPreviouslyCompiledFunctions("large_module");
            Assert.Equal(50, functions.Count);

            for (var i = 0; i < 50; i++) Assert.True(jitCache.WasCompiledPreviously("large_module", i));
        }
        finally
        {
            cleanup(tempDir);
        }
    }

    /// <summary>
    ///     测试缓存失效后重新记录
    /// </summary>
    [Fact]
    public void Invalidate_ThenRecord_WorksCorrectly()
    {
        var (cache, tempDir) = create_test_cache();
        try
        {
            var jitCache = new LightDbJitCache(cache);

            jitCache.RecordCompilation("test_module", 0);
            jitCache.RecordCompilation("test_module", 1);

            jitCache.Invalidate("test_module");

            Assert.False(jitCache.WasCompiledPreviously("test_module", 0));

            jitCache.RecordCompilation("test_module", 0);

            Assert.True(jitCache.WasCompiledPreviously("test_module", 0));
            Assert.False(jitCache.WasCompiledPreviously("test_module", 1));
        }
        finally
        {
            cleanup(tempDir);
        }
    }
}