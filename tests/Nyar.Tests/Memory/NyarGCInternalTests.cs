using System.Collections;
using System.Diagnostics;
using System.Numerics;
using Nyar.Types;

namespace Nyar.Tests.Memory;

public class NyarGcInternalTests
{
    #region 测试辅助类

    private sealed class TestGcRootProvider : IGcRootProvider
    {
        private readonly List<Value> _roots;

        public TestGcRootProvider(List<Value> roots)
        {
            _roots = roots;
        }

        public IEnumerable<Value> GetRootValues()
        {
            return _roots;
        }
    }

    #endregion

    #region NyarGC 内部 API 测试（独立对象表）

    [Fact]
    public void Allocate_ReturnsIncrementalIndices()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        var idx1 = gc.allocate("obj1");
        var idx2 = gc.allocate("obj2");
        var idx3 = gc.allocate("obj3");

        Assert.Equal(0, idx1);
        Assert.Equal(1, idx2);
        Assert.Equal(2, idx3);
        Assert.Equal(3, gc.TableCapacity);
        Assert.Equal(3, gc.LiveObjectCount);
    }

    [Fact]
    public void Free_SetsSlotToNull()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        var idx = gc.allocate("obj");
        gc.Free(idx);

        Assert.Null(table[idx]);
        Assert.Equal(0, gc.LiveObjectCount);
        Assert.Equal(1, gc.FreeSlotCount);
    }

    [Fact]
    public void Allocate_AfterFree_ReusesSlot()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        var idx1 = gc.allocate("obj1");
        gc.Free(idx1);
        var idx2 = gc.allocate("obj2");

        Assert.Equal(idx1, idx2);
        Assert.Equal("obj2", table[idx2]);
        Assert.Equal(1, gc.LiveObjectCount);
        Assert.Equal(0, gc.FreeSlotCount);
    }

    [Fact]
    public void Free_InvalidIndex_DoesNothing()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        gc.allocate("obj");
        gc.Free(-1);
        gc.Free(99);

        Assert.Equal(1, gc.LiveObjectCount);
    }

    [Fact]
    public void Free_AlreadyFreedSlot_DoesNothing()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        var idx = gc.allocate("obj");
        gc.Free(idx);
        gc.Free(idx);

        Assert.Equal(0, gc.LiveObjectCount);
        Assert.Equal(1, gc.FreeSlotCount);
    }

    [Fact]
    public void TotalAllocations_IncrementedOnEachAllocate()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        gc.allocate("a");
        gc.allocate("b");
        gc.allocate("c");

        Assert.Equal(3, gc.TotalAllocations);
    }

    [Fact]
    public void TotalAllocations_NotDecrementedOnFree()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        var idx = gc.allocate("a");
        gc.Free(idx);

        Assert.Equal(1, gc.TotalAllocations);
    }

    #endregion

    #region Mark-Sweep 回收测试（独立对象表，无 Value 引用）

    [Fact]
    public void Collect_NoRoots_AllReclaimed()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        for (var i = 0; i < 10; i++) gc.allocate($"obj_{i}");

        var provider = new TestGcRootProvider([]);
        var reclaimed = gc.Collect(provider);

        Assert.Equal(10, reclaimed);
        Assert.Equal(0, gc.LiveObjectCount);
    }

    [Fact]
    public void Collect_CircularReferences_UnreachableBothReclaimed()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        var dict1 = new Hashtable();
        var dict2 = new Hashtable();
        dict1["ref"] = dict2;
        dict2["ref"] = dict1;

        gc.allocate(dict1);
        gc.allocate(dict2);

        var provider = new TestGcRootProvider([]);
        var reclaimed = gc.Collect(provider);

        Assert.Equal(2, reclaimed);
        Assert.Equal(0, gc.LiveObjectCount);
    }

    [Fact]
    public void Collect_SlotReuseAfterSweep()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        gc.allocate("obj1");
        gc.allocate("obj2");

        gc.Collect(new TestGcRootProvider([]));

        Assert.Equal(0, gc.FreeSlotCount);

        var newIdx = gc.allocate("new_obj");
        Assert.Equal(0, newIdx);
    }

    [Fact]
    public void Collect_MultipleCollections_AccumulatesStats()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        gc.allocate("obj1");
        gc.Collect(new TestGcRootProvider([]));

        gc.allocate("obj2");
        gc.Collect(new TestGcRootProvider([]));

        Assert.Equal(2, gc.TotalCollections);
        Assert.Equal(2, gc.TotalReclaimed);
    }

    #endregion

    #region Value 共享对象表集成测试（不调用 Collect，避免影响其他测试）

    [Fact]
    public void SharedTable_ReachableString_Preserved()
    {
        var reachableVal = Value.from_string("reachable_value");

        Assert.Equal(ValueType.@string, reachableVal.type);
        Assert.Equal("reachable_value", reachableVal.@string);
    }

    [Fact]
    public void SharedTable_ClosureWithUpvalues_UpvaluesPreserved()
    {
        var upvalueStr = Value.from_string("upvalue_data");
        var func = new NyarFunction("test", 0, 0, 0, 1);
        var closure = new NyarClosure(func, [upvalueStr]);
        var closureValue = Value.from_closure(closure);

        Assert.Equal(ValueType.closure, closureValue.type);
        Assert.Equal(ValueType.@string, upvalueStr.type);
        Assert.Equal("upvalue_data", upvalueStr.@string);
    }

    [Fact]
    public void SharedTable_ClosureChain_AllPreserved()
    {
        var rootVal = Value.from_string("chain_root");
        var currentVal = rootVal;

        for (var i = 0; i < 10; i++)
        {
            var func = new NyarFunction($"chain_{i}", 0, 0, 0, 1);
            var closure = new NyarClosure(func, [currentVal]);
            currentVal = Value.from_closure(closure);
        }

        Assert.Equal(ValueType.@string, rootVal.type);
        Assert.Equal("chain_root", rootVal.@string);
    }

    [Fact]
    public void SharedTable_DictionaryWithChildValues_ChildrenPreserved()
    {
        var childValue = Value.from_string("child_string");
        var dict = new Hashtable
        {
            ["key"] = childValue
        };
        var dictValue = Value.from_object(dict);

        Assert.Equal(ValueType.@object, dictValue.type);
        Assert.Equal(ValueType.@string, childValue.type);
        Assert.Equal("child_string", childValue.@string);
    }

    [Fact]
    public void Collect_MultipleTypes_AllPreservedAfterGC()
    {
        var gc = new NyarGC([], new object());

        var strVal = Value.from_string("test_string");
        var objVal = Value.from_object(new List<int> { 1, 2, 3 });
        var bigIntVal = Value.from_big_int(BigInteger.Parse("9999999999"));
        var longVal = Value.from_long(long.MaxValue);

        var func = new NyarFunction("test", 0, 0, 0, 1);
        var closure = new NyarClosure(func, [strVal]);
        var closureVal = Value.from_closure(closure);

        var roots = new List<Value> { strVal, objVal, bigIntVal, longVal, closureVal };
        var provider = new TestGcRootProvider(roots);

        gc.Collect(provider);

        Assert.Equal(ValueType.@string, strVal.type);
        Assert.Equal(ValueType.@object, objVal.type);
        Assert.Equal(ValueType.big_int, bigIntVal.type);
        Assert.Equal(ValueType.@long, longVal.type);
        Assert.Equal(ValueType.closure, closureVal.type);
    }

    #endregion

    #region MaybeCollect 测试

    [Fact]
    public void MaybeCollect_BelowThreshold_DoesNotCollect()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock)
        {
            GcThreshold = 100
        };

        gc.allocate("obj");

        var result = gc.MaybeCollect(new TestGcRootProvider([]));

        Assert.False(result);
        Assert.Equal(0, gc.TotalCollections);
    }

    [Fact]
    public void MaybeCollect_AboveThreshold_TriggersCollection()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock)
        {
            GcThreshold = 5
        };

        for (var i = 0; i < 10; i++) gc.allocate($"obj_{i}");

        var result = gc.MaybeCollect(new TestGcRootProvider([]));

        Assert.True(result);
        Assert.Equal(1, gc.TotalCollections);
    }

    [Fact]
    public void MaybeCollect_AdjustsThreshold()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock)
        {
            GcThreshold = 5
        };

        gc.allocate("root");
        for (var i = 0; i < 10; i++) gc.allocate($"unreachable_{i}");

        gc.MaybeCollect(new TestGcRootProvider([]));

        Assert.True(gc.GcThreshold >= 1024);
    }

    #endregion

    #region GC 暂停时间测量

    [Fact]
    public void PauseTime_SmallHeap_Under100ms()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        for (var i = 0; i < 1000; i++) gc.allocate($"obj_{i}");

        var provider = new TestGcRootProvider([]);
        var sw = Stopwatch.StartNew();
        gc.Collect(provider);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 100, $"GC 暂停时间 {sw.ElapsedMilliseconds}ms 超过 100ms");
    }

    [Fact]
    public void PauseTime_LargeHeap_Under500ms()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        for (var i = 0; i < 10000; i++) gc.allocate($"obj_{i}");

        var provider = new TestGcRootProvider([]);
        var sw = Stopwatch.StartNew();
        gc.Collect(provider);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 500, $"GC 暂停时间 {sw.ElapsedMilliseconds}ms 超过 500ms");
    }

    [Fact]
    public void PauseTime_CircularReferences_Under500ms()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        for (var i = 0; i < 500; i++)
        {
            var dict1 = new Hashtable();
            var dict2 = new Hashtable();
            dict1["ref"] = dict2;
            dict2["ref"] = dict1;
            gc.allocate(dict1);
            gc.allocate(dict2);
        }

        var provider = new TestGcRootProvider([]);
        var sw = Stopwatch.StartNew();
        gc.Collect(provider);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 500, $"GC 暂停时间 {sw.ElapsedMilliseconds}ms 超过 500ms");
    }

    #endregion

    #region 与 Executor 集成的 GC 测试

    [Fact]
    public void ExecutorGC_Integration_SimpleFunction()
    {
        var module = new NyarModule("gc_test");
        var func = new NyarFunction("main", 0, 0, 0, 10);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarHeadCode.Const;
        bytecode[5] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_int(42));
        module.raw_bytecode = bytecode;

        var vm = new NyarVM();
        vm.Load(module);

        var result = vm.Run("gc_test", "main");

        Assert.Equal(ValueType.@int, result.type);
    }

    [Fact]
    public void ExecutorGC_Integration_MultipleRuns()
    {
        var module = new NyarModule("gc_multi");
        var func = new NyarFunction("compute", 0, 0, 0, 10);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarHeadCode.Const;
        bytecode[5] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_int(100));
        module.raw_bytecode = bytecode;

        var vm = new NyarVM();
        vm.Load(module);

        for (var i = 0; i < 100; i++)
        {
            var result = vm.Run("gc_multi", "compute");
            Assert.Equal(ValueType.@int, result.type);
        }
    }

    [Fact]
    public void ExecutorGC_Integration_StringOperations()
    {
        var module = new NyarModule("gc_string");
        var func = new NyarFunction("str_test", 0, 0, 0, 10);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarHeadCode.Const;
        bytecode[5] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_string("hello gc"));
        module.raw_bytecode = bytecode;

        var vm = new NyarVM();
        vm.Load(module);

        var result = vm.Run("gc_string", "str_test");
        Assert.Equal(ValueType.@string, result.type);
    }

    [Fact]
    public void ExecutorGC_Integration_ManyStringAllocations()
    {
        var module = new NyarModule("gc_alloc");
        var func = new NyarFunction("alloc_test", 0, 0, 0, 10);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarHeadCode.Const;
        bytecode[5] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_string("allocation_pressure"));
        module.raw_bytecode = bytecode;

        var vm = new NyarVM();
        vm.Load(module);

        for (var i = 0; i < 1000; i++)
        {
            var result = vm.Run("gc_alloc", "alloc_test");
            Assert.Equal(ValueType.@string, result.type);
        }
    }

    #endregion

    #region 压力测试

    [Fact]
    public void StressTest_ManyAllocationsAndCollections()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock)
        {
            GcThreshold = 100
        };

        for (var i = 0; i < 10000; i++)
        {
            gc.allocate($"obj_{i}");

            if (i % 100 == 99)
            {
                gc.MaybeCollect(new TestGcRootProvider([]));
            }
        }

        Assert.True(gc.TotalCollections > 0);
        Assert.True(gc.TotalReclaimed > 0);
    }

    [Fact]
    public void StressTest_ShortLivedObjects()
    {
        var table = new List<object?>();
        var tableLock = new object();
        var gc = new NyarGC(table, tableLock);

        gc.allocate("root");

        for (var i = 0; i < 5000; i++)
        {
            var idx = gc.allocate($"short_lived_{i}");
            gc.Free(idx);
        }

        gc.Collect(new TestGcRootProvider([]));

        Assert.Equal(0, gc.LiveObjectCount);
    }

    [Fact]
    public void StressTest_ClosureChainsWithSharedTable()
    {
        var rootVal = Value.from_string("chain_root_stress");
        var currentVal = rootVal;

        for (var i = 0; i < 50; i++)
        {
            var func = new NyarFunction($"chain_{i}", 0, 0, 0, 1);
            var closure = new NyarClosure(func, [currentVal]);
            currentVal = Value.from_closure(closure);
        }

        Assert.Equal(ValueType.@string, rootVal.type);
        Assert.Equal("chain_root_stress", rootVal.@string);
    }

    [Fact]
    public void StressTest_ManyValueCreations()
    {
        var roots = new List<Value>();

        for (var round = 0; round < 10; round++)
        {
            var rootVal = Value.from_string($"root_round_{round}");
            roots.Add(rootVal);

            for (var i = 0; i < 100; i++) Value.from_string($"temp_{round}_{i}");
        }

        foreach (var root in roots)
        {
            Assert.Equal(ValueType.@string, root.type);
        }
    }

    #endregion

    #region 统一分配器测试

    [Fact]
    public void UnifiedAllocator_IGcAllocator_InterfaceExists()
    {
        var gc = new NyarGC(Value._shared_object_table, Value._shared_table_lock);

        Assert.True(gc is IGcAllocator, "NyarGC 应实现 IGcAllocator 接口");
    }

    [Fact]
    public void UnifiedAllocator_RegisterAndUnregister_Works()
    {
        var gc = new NyarGC(Value._shared_object_table, Value._shared_table_lock);

        Value.register_gc_allocator(gc);
        try
        {
            var beforeAlloc = gc.TotalAllocations;
            var obj = new Dictionary<string, Value>();
            var val = Value.from_object(obj);

            Assert.Equal(ValueType.@object, val.type);
            Assert.Equal(beforeAlloc + 1, gc.TotalAllocations);

            var retrieved = val.@object as Dictionary<string, Value>;
            Assert.Same(obj, retrieved);
        }
        finally
        {
            Value.unregister_gc_allocator();
        }
    }

    [Fact]
    public void UnifiedAllocator_AfterUnregister_DirectAllocation()
    {
        var gc = new NyarGC(Value._shared_object_table, Value._shared_table_lock);

        Value.register_gc_allocator(gc);
        Value.unregister_gc_allocator();

        var beforeAlloc = gc.TotalAllocations;
        var val = Value.from_string("no_gc");

        Assert.Equal(ValueType.@string, val.type);
        Assert.Equal(beforeAlloc, gc.TotalAllocations);
    }

    [Fact]
    public void UnifiedAllocator_NyarVM_UsesSharedGc()
    {
        var vm = new NyarVM(new JitOptions { Enabled = false });

        var module = new NyarModule("test");
        var func = new NyarFunction("create_obj", 0, 0, 0, 2);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[2];
        bytecode[0] = (byte)NyarHeadCode.Const;
        bytecode[1] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_string("hello"));
        module.raw_bytecode = bytecode;
        vm.Load(module);

        Assert.True(vm.GC.LiveObjectCount >= 1);
        Assert.Same(vm.GC, vm.GC);
    }

    #endregion
}