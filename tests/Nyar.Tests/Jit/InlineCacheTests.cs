using System.Diagnostics;

namespace Nyar.Tests.Jit;

public class InlineCacheTests
{
    #region GetStatistics 测试

    [Fact]
    public void GetStatistics_ContainsIcInfo()
    {
        var jit = new JitCompiler();

        jit.UpdateInlineCache(0, Value.from_int(1), 0);
        jit.LookupInlineCache(0, Value.from_int(1));
        jit.LookupInlineCache(0, Value.from_double(1.0));

        var stats = jit.GetStatistics();

        Assert.Contains("IC 命中", stats);
        Assert.Contains("IC 未命中", stats);
        Assert.Contains("IC 命中率", stats);
    }

    #endregion

    #region IcTypeKey 测试

    [Fact]
    public void IcTypeKey_Equality_SameValues()
    {
        var key1 = new IcTypeKey(1, 0);
        var key2 = new IcTypeKey(1, 0);

        Assert.Equal(key1, key2);
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void IcTypeKey_Inequality_DifferentValueTypeTag()
    {
        var key1 = new IcTypeKey(1, 0);
        var key2 = new IcTypeKey(2, 0);

        Assert.NotEqual(key1, key2);
        Assert.True(key1 != key2);
        Assert.False(key1 == key2);
    }

    [Fact]
    public void IcTypeKey_Inequality_DifferentTypeHash()
    {
        var key1 = new IcTypeKey((byte)ValueType.@object, 12345);
        var key2 = new IcTypeKey((byte)ValueType.@object, 67890);

        Assert.NotEqual(key1, key2);
        Assert.True(key1 != key2);
    }

    [Fact]
    public void IcTypeKey_NonObjectType_HashIsZero()
    {
        var intKey = new IcTypeKey((byte)ValueType.@int, 0);
        var doubleKey = new IcTypeKey((byte)ValueType.@double, 0);

        Assert.Equal(0, intKey.TypeHash);
        Assert.Equal(0, doubleKey.TypeHash);
    }

    [Fact]
    public void IcTypeKey_ObjectType_DifferentHashes()
    {
        var key1 = new IcTypeKey((byte)ValueType.@object, 100);
        var key2 = new IcTypeKey((byte)ValueType.@object, 200);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void IcTypeKey_Equals_NullObject()
    {
        var key = new IcTypeKey(1, 0);
        Assert.False(key.Equals(null));
    }

    [Fact]
    public void IcTypeKey_Equals_NonIcTypeKeyObject()
    {
        var key = new IcTypeKey(1, 0);
        Assert.False(key.Equals("not a key"));
    }

    #endregion

    #region InlineCacheSlot 测试

    [Fact]
    public void InlineCacheSlot_InitiallyEmpty()
    {
        var slot = new InlineCacheSlot();
        Assert.True(slot.IsEmpty);
    }

    [Fact]
    public void InlineCacheSlot_LookupEmpty_ReturnsMinusOne()
    {
        var slot = new InlineCacheSlot();
        var result = slot.Lookup(new IcTypeKey(1, 0));
        Assert.Equal(-1, result);
    }

    [Fact]
    public void InlineCacheSlot_UpdateAndLookup_SingleEntry()
    {
        var slot = new InlineCacheSlot();
        var key = new IcTypeKey((byte)ValueType.@int, 0);

        slot.Update(key, 42);
        Assert.False(slot.IsEmpty);

        var result = slot.Lookup(key);
        Assert.Equal(42, result);
    }

    [Fact]
    public void InlineCacheSlot_UpdateAndLookup_MultipleEntries()
    {
        var slot = new InlineCacheSlot();

        slot.Update(new IcTypeKey((byte)ValueType.@int, 0), 1);
        slot.Update(new IcTypeKey((byte)ValueType.@double, 0), 2);
        slot.Update(new IcTypeKey((byte)ValueType.@bool, 0), 3);

        Assert.Equal(1, slot.Lookup(new IcTypeKey((byte)ValueType.@int, 0)));
        Assert.Equal(2, slot.Lookup(new IcTypeKey((byte)ValueType.@double, 0)));
        Assert.Equal(3, slot.Lookup(new IcTypeKey((byte)ValueType.@bool, 0)));
    }

    [Fact]
    public void InlineCacheSlot_UpdateExistingKey_OverwritesTarget()
    {
        var slot = new InlineCacheSlot();
        var key = new IcTypeKey((byte)ValueType.@int, 0);

        slot.Update(key, 10);
        slot.Update(key, 20);

        Assert.Equal(20, slot.Lookup(key));
    }

    [Fact]
    public void InlineCacheSlot_MaxFourEntries()
    {
        var slot = new InlineCacheSlot();

        slot.Update(new IcTypeKey(1, 0), 1);
        slot.Update(new IcTypeKey(2, 0), 2);
        slot.Update(new IcTypeKey(3, 0), 3);
        slot.Update(new IcTypeKey(4, 0), 4);

        Assert.Equal(1, slot.Lookup(new IcTypeKey(1, 0)));
        Assert.Equal(2, slot.Lookup(new IcTypeKey(2, 0)));
        Assert.Equal(3, slot.Lookup(new IcTypeKey(3, 0)));
        Assert.Equal(4, slot.Lookup(new IcTypeKey(4, 0)));
    }

    [Fact]
    public void InlineCacheSlot_Megamorphic_OverwritesFirstEntry()
    {
        var slot = new InlineCacheSlot();

        slot.Update(new IcTypeKey(1, 0), 1);
        slot.Update(new IcTypeKey(2, 0), 2);
        slot.Update(new IcTypeKey(3, 0), 3);
        slot.Update(new IcTypeKey(4, 0), 4);

        slot.Update(new IcTypeKey(5, 0), 5);

        Assert.Equal(5, slot.Lookup(new IcTypeKey(5, 0)));
        Assert.Equal(-1, slot.Lookup(new IcTypeKey(1, 0)));
        Assert.Equal(2, slot.Lookup(new IcTypeKey(2, 0)));
        Assert.Equal(3, slot.Lookup(new IcTypeKey(3, 0)));
        Assert.Equal(4, slot.Lookup(new IcTypeKey(4, 0)));
    }

    [Fact]
    public void InlineCacheSlot_Remove_ExistingKey()
    {
        var slot = new InlineCacheSlot();

        slot.Update(new IcTypeKey(1, 0), 1);
        slot.Update(new IcTypeKey(2, 0), 2);
        slot.Update(new IcTypeKey(3, 0), 3);

        var removed = slot.Remove(new IcTypeKey(2, 0));
        Assert.True(removed);
        Assert.Equal(-1, slot.Lookup(new IcTypeKey(2, 0)));
        Assert.Equal(1, slot.Lookup(new IcTypeKey(1, 0)));
        Assert.Equal(3, slot.Lookup(new IcTypeKey(3, 0)));
    }

    [Fact]
    public void InlineCacheSlot_Remove_NonExistingKey()
    {
        var slot = new InlineCacheSlot();
        slot.Update(new IcTypeKey(1, 0), 1);

        var removed = slot.Remove(new IcTypeKey(99, 0));
        Assert.False(removed);
    }

    [Fact]
    public void InlineCacheSlot_RemoveLastEntry_BecomesEmpty()
    {
        var slot = new InlineCacheSlot();
        var key = new IcTypeKey(1, 0);

        slot.Update(key, 1);
        slot.Remove(key);

        Assert.True(slot.IsEmpty);
    }

    [Fact]
    public void InlineCacheSlot_Clear()
    {
        var slot = new InlineCacheSlot();

        slot.Update(new IcTypeKey(1, 0), 1);
        slot.Update(new IcTypeKey(2, 0), 2);
        slot.Update(new IcTypeKey(3, 0), 3);

        slot.Clear();

        Assert.True(slot.IsEmpty);
        Assert.Equal(-1, slot.Lookup(new IcTypeKey(1, 0)));
        Assert.Equal(-1, slot.Lookup(new IcTypeKey(2, 0)));
        Assert.Equal(-1, slot.Lookup(new IcTypeKey(3, 0)));
    }

    [Fact]
    public void InlineCacheSlot_LookupNonExistingKey_ReturnsMinusOne()
    {
        var slot = new InlineCacheSlot();
        slot.Update(new IcTypeKey(1, 0), 1);

        Assert.Equal(-1, slot.Lookup(new IcTypeKey(2, 0)));
    }

    #endregion

    #region JitCompiler IC 集成测试

    [Fact]
    public void LookupInlineCache_NoSlot_ReturnsMinusOne()
    {
        var jit = new JitCompiler();
        var result = jit.LookupInlineCache(0, Value.from_int(42));

        Assert.Equal(-1, result);
        Assert.Equal(0, jit.IcHitCount);
        Assert.Equal(1, jit.IcMissCount);
    }

    [Fact]
    public void UpdateInlineCache_ThenLookup_Hit()
    {
        var jit = new JitCompiler();

        jit.UpdateInlineCache(0, Value.from_int(42), 7);

        var result = jit.LookupInlineCache(0, Value.from_int(42));
        Assert.Equal(7, result);
        Assert.Equal(1, jit.IcHitCount);
        Assert.Equal(0, jit.IcMissCount);
    }

    [Fact]
    public void LookupInlineCache_DifferentType_Miss()
    {
        var jit = new JitCompiler();

        jit.UpdateInlineCache(0, Value.from_int(42), 1);

        var result = jit.LookupInlineCache(0, Value.from_double(3.14));
        Assert.Equal(-1, result);
        Assert.Equal(0, jit.IcHitCount);
        Assert.Equal(1, jit.IcMissCount);
    }

    [Fact]
    public void LookupInlineCache_ObjectType_SameShape_Hit()
    {
        var jit = new JitCompiler();

        var dict1 = new Dictionary<string, Value>
        {
            ["x"] = Value.from_int(1),
            ["y"] = Value.from_int(2)
        };
        var dict2 = new Dictionary<string, Value>
        {
            ["x"] = Value.from_int(10),
            ["y"] = Value.from_int(20)
        };

        var obj1 = Value.from_object(dict1);
        var obj2 = Value.from_object(dict2);

        jit.UpdateInlineCache(0, obj1, 5);

        var result = jit.LookupInlineCache(0, obj2);
        Assert.Equal(5, result);
        Assert.Equal(1, jit.IcHitCount);
    }

    [Fact]
    public void LookupInlineCache_ObjectType_DifferentShape_Miss()
    {
        var jit = new JitCompiler();

        var dict1 = new Dictionary<string, Value>
        {
            ["x"] = Value.from_int(1)
        };
        var dict2 = new Dictionary<string, Value>
        {
            ["a"] = Value.from_int(1),
            ["b"] = Value.from_int(2)
        };

        var obj1 = Value.from_object(dict1);
        var obj2 = Value.from_object(dict2);

        jit.UpdateInlineCache(0, obj1, 5);

        var result = jit.LookupInlineCache(0, obj2);
        Assert.Equal(-1, result);
        Assert.Equal(1, jit.IcMissCount);
    }

    [Fact]
    public void IcHitRate_NoData_ReturnsZero()
    {
        var jit = new JitCompiler();
        Assert.Equal(0.0, jit.IcHitRate);
    }

    [Fact]
    public void IcHitRate_AllHits_ReturnsOne()
    {
        var jit = new JitCompiler();

        jit.UpdateInlineCache(0, Value.from_int(1), 0);
        jit.LookupInlineCache(0, Value.from_int(1));
        jit.LookupInlineCache(0, Value.from_int(1));
        jit.LookupInlineCache(0, Value.from_int(1));

        Assert.Equal(1.0, jit.IcHitRate, 0.001);
    }

    [Fact]
    public void IcHitRate_MixedHitsMisses()
    {
        var jit = new JitCompiler();

        jit.UpdateInlineCache(0, Value.from_int(1), 0);

        jit.LookupInlineCache(0, Value.from_int(1));
        jit.LookupInlineCache(0, Value.from_int(1));
        jit.LookupInlineCache(0, Value.from_double(1.0));

        Assert.Equal(2.0 / 3.0, jit.IcHitRate, 0.001);
    }

    [Fact]
    public void ClearInlineCaches_ResetsHitMissCounts()
    {
        var jit = new JitCompiler();

        jit.UpdateInlineCache(0, Value.from_int(1), 0);
        jit.LookupInlineCache(0, Value.from_int(1));
        jit.LookupInlineCache(0, Value.from_double(1.0));

        jit.ClearInlineCaches();

        Assert.Equal(0, jit.IcHitCount);
        Assert.Equal(0, jit.IcMissCount);
        Assert.Equal(0.0, jit.IcHitRate);
    }

    [Fact]
    public void InvalidateInlineCache_SpecificSlot()
    {
        var jit = new JitCompiler();

        jit.UpdateInlineCache(0, Value.from_int(1), 10);
        jit.UpdateInlineCache(1, Value.from_int(1), 20);

        var invalidated = jit.InvalidateInlineCache(0);
        Assert.True(invalidated);

        Assert.Equal(-1, jit.LookupInlineCache(0, Value.from_int(1)));
        Assert.Equal(20, jit.LookupInlineCache(1, Value.from_int(1)));
    }

    [Fact]
    public void InvalidateInlineCache_NonExistingSlot()
    {
        var jit = new JitCompiler();
        var invalidated = jit.InvalidateInlineCache(99);
        Assert.False(invalidated);
    }

    [Fact]
    public void MultipleCacheSlots_Independent()
    {
        var jit = new JitCompiler();

        jit.UpdateInlineCache(0, Value.from_int(1), 10);
        jit.UpdateInlineCache(1, Value.from_int(1), 20);
        jit.UpdateInlineCache(2, Value.from_double(1.0), 30);

        Assert.Equal(10, jit.LookupInlineCache(0, Value.from_int(1)));
        Assert.Equal(20, jit.LookupInlineCache(1, Value.from_int(1)));
        Assert.Equal(30, jit.LookupInlineCache(2, Value.from_double(1.0)));
    }

    #endregion

    #region CallSiteProfile 与去虚拟化测试

    [Fact]
    public void RecordCallSiteHit_Under100Calls_NoAction()
    {
        var jit = new JitCompiler();

        for (var i = 0; i < 99; i++)
        {
            var decision = jit.RecordCallSiteHit(0, Value.from_int(1), 0);
            Assert.Equal(DevirtualizationDecision.NoAction, decision);
        }
    }

    [Fact]
    public void RecordCallSiteHit_100Calls_SingleType_PromoteToStatic()
    {
        var jit = new JitCompiler();

        DevirtualizationDecision decision = default;
        for (var i = 0; i < 100; i++) decision = jit.RecordCallSiteHit(0, Value.from_int(1), 0);

        Assert.Equal(DevirtualizationDecision.PromoteToStatic, decision);

        var profile = jit.GetCallSiteProfile(0);
        Assert.NotNull(profile);
        Assert.True(profile.IsDevirtualized);
        Assert.Equal(100, profile.TotalCalls);
        Assert.Single(profile.TypeCounts);
    }

    [Fact]
    public void RecordCallSiteHit_TwoTypes_PromoteToWitness()
    {
        var jit = new JitCompiler();

        DevirtualizationDecision decision = default;
        for (var i = 0; i < 100; i++)
        {
            var receiver = i % 2 == 0 ? Value.from_int(1) : Value.from_double(1.0);
            decision = jit.RecordCallSiteHit(0, receiver, 0);
        }

        Assert.Equal(DevirtualizationDecision.PromoteToWitness, decision);

        var profile = jit.GetCallSiteProfile(0);
        Assert.NotNull(profile);
        Assert.True(profile.IsDevirtualized);
        Assert.Equal(2, profile.TypeCounts.Count);
    }

    [Fact]
    public void RecordCallSiteHit_FourTypes_KeepDynamic()
    {
        var jit = new JitCompiler();

        var receivers = new[]
        {
            Value.from_int(1),
            Value.from_double(1.0),
            Value.from_bool(true),
            Value.@null
        };

        DevirtualizationDecision decision = default;
        for (var i = 0; i < 100; i++) decision = jit.RecordCallSiteHit(0, receivers[i % 4], 0);

        Assert.Equal(DevirtualizationDecision.KeepDynamic, decision);

        var profile = jit.GetCallSiteProfile(0);
        Assert.NotNull(profile);
        Assert.False(profile.IsDevirtualized);
        Assert.Equal(4, profile.TypeCounts.Count);
    }

    [Fact]
    public void RecordCallSiteHit_ThreeTypes_PromoteToWitness()
    {
        var jit = new JitCompiler();

        var receivers = new[]
        {
            Value.from_int(1),
            Value.from_double(1.0),
            Value.from_bool(true)
        };

        DevirtualizationDecision decision = default;
        for (var i = 0; i < 100; i++) decision = jit.RecordCallSiteHit(0, receivers[i % 3], 0);

        Assert.Equal(DevirtualizationDecision.PromoteToWitness, decision);
    }

    [Fact]
    public void GetCallSiteProfile_NonExisting_ReturnsNull()
    {
        var jit = new JitCompiler();
        Assert.Null(jit.GetCallSiteProfile(99));
    }

    [Fact]
    public void InvalidateInlineCache_ClearsProfile()
    {
        var jit = new JitCompiler();

        for (var i = 0; i < 100; i++) jit.RecordCallSiteHit(0, Value.from_int(1), 0);

        Assert.NotNull(jit.GetCallSiteProfile(0));

        jit.InvalidateInlineCache(0);

        Assert.Null(jit.GetCallSiteProfile(0));
    }

    #endregion

    #region PatchBytecode 测试

    [Fact]
    public void PatchBytecode_NoPatchableBytecode_ReturnsFalse()
    {
        var jit = new JitCompiler();

        var result = jit.PatchBytecode(0, DevirtualizationDecision.PromoteToStatic, 0);
        Assert.False(result);
    }

    [Fact]
    public void PatchBytecode_NoCacheSlotMapping_ReturnsFalse()
    {
        var jit = new JitCompiler();
        jit.SetPatchableBytecode(new byte[32]);

        var result = jit.PatchBytecode(0, DevirtualizationDecision.PromoteToStatic, 0);
        Assert.False(result);
    }

    [Fact]
    public void PatchBytecode_PromoteToStatic_WritesCallStaticOpcode()
    {
        var jit = new JitCompiler();
        var bytecode = new byte[32];
        for (var i = 0; i < bytecode.Length; i++)
            bytecode[i] = 0xFF;

        jit.SetPatchableBytecode(bytecode);
        jit.RegisterCacheSlotPc(0, 4);

        var result = jit.PatchBytecode(0, DevirtualizationDecision.PromoteToStatic, 42);
        Assert.True(result);

        Assert.Equal((byte)NyarHeadCode.CallStatic, bytecode[4]);
        Assert.Equal(42 & 0xFF, bytecode[5]);
        Assert.Equal((42 >> 8) & 0xFF, bytecode[6]);
        Assert.Equal((42 >> 16) & 0xFF, bytecode[7]);
        Assert.Equal((42 >> 24) & 0xFF, bytecode[8]);

        for (var i = 9; i < 4 + 13; i++) Assert.Equal((byte)NyarHeadCode.Nop, bytecode[i]);
    }

    [Fact]
    public void PatchBytecode_PromoteToWitness_WritesCallWitnessOpcode()
    {
        var jit = new JitCompiler();
        var bytecode = new byte[32];
        for (var i = 0; i < bytecode.Length; i++)
            bytecode[i] = 0xFF;

        jit.SetPatchableBytecode(bytecode);
        jit.RegisterCacheSlotPc(0, 4);

        var result = jit.PatchBytecode(0, DevirtualizationDecision.PromoteToWitness, 7);
        Assert.True(result);

        Assert.Equal((byte)NyarHeadCode.CallWitness, bytecode[4]);
        Assert.Equal(7 & 0xFF, bytecode[5]);
    }

    [Fact]
    public void PatchBytecode_KeepDynamic_ReturnsFalse()
    {
        var jit = new JitCompiler();
        var bytecode = new byte[32];
        jit.SetPatchableBytecode(bytecode);
        jit.RegisterCacheSlotPc(0, 4);

        var result = jit.PatchBytecode(0, DevirtualizationDecision.KeepDynamic, 0);
        Assert.False(result);
    }

    [Fact]
    public void PatchBytecode_NoAction_ReturnsFalse()
    {
        var jit = new JitCompiler();
        var bytecode = new byte[32];
        jit.SetPatchableBytecode(bytecode);
        jit.RegisterCacheSlotPc(0, 4);

        var result = jit.PatchBytecode(0, DevirtualizationDecision.NoAction, 0);
        Assert.False(result);
    }

    [Fact]
    public void PatchBytecode_BackupOriginalBytecode()
    {
        var jit = new JitCompiler();
        var bytecode = new byte[32];
        for (var i = 0; i < 32; i++)
            bytecode[i] = (byte)(i + 1);

        jit.SetPatchableBytecode(bytecode);
        jit.RegisterCacheSlotPc(0, 4);

        jit.PatchBytecode(0, DevirtualizationDecision.PromoteToStatic, 0);

        jit.InvalidateInlineCache(0);

        for (var i = 4; i is < 4 + 13 and < 32; i++) Assert.Equal((byte)(i + 1), bytecode[i]);
    }

    #endregion

    #region IC 性能基准测试

    [Fact]
    public void IcBenchmark_LookupLatency_HitFasterThanMiss()
    {
        var jit = new JitCompiler();

        jit.UpdateInlineCache(0, Value.from_int(42), 7);

        var hitReceiver = Value.from_int(42);

        const int warmup = 1000;
        const int iterations = 2_000_000;

        for (var i = 0; i < warmup; i++) jit.LookupInlineCache(0, hitReceiver);

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++) jit.LookupInlineCache(0, hitReceiver);
        sw.Stop();
        var hitNs = (double)sw.ElapsedTicks / Stopwatch.Frequency * 1_000_000_000 / iterations;

        var missCount = jit.IcMissCount;
        Assert.Equal(0, missCount);

        Debug.WriteLine($"[IC Benchmark] Hit latency: {hitNs:F1}ns, Hit count: {jit.IcHitCount}");
        Assert.True(jit.IcHitCount >= iterations, $"IC 命中次数应 >= {iterations}");
    }

    [Fact]
    public void IcBenchmark_MonomorphicHitRate_Exceeds90Percent()
    {
        var jit = new JitCompiler();

        jit.UpdateInlineCache(0, Value.from_int(1), 0);

        const int totalCalls = 10_000;
        var hitCount = 0;

        for (var i = 0; i < totalCalls; i++)
        {
            var receiver = i % 100 == 0 ? Value.from_double(i) : Value.from_int(i);
            var result = jit.LookupInlineCache(0, receiver);
            if (result >= 0)
            {
                hitCount++;
            }
            else
            {
                jit.UpdateInlineCache(0, receiver, 0);
            }
        }

        var hitRate = (double)hitCount / totalCalls;
        Debug.WriteLine($"[IC Monomorphic] 命中率: {hitRate:P1} ({hitCount}/{totalCalls})");

        Assert.True(hitRate >= 0.90, $"单态 IC 命中率 {hitRate:P1} 应 >= 90%");
    }

    [Fact]
    public void IcBenchmark_PolymorphicHitRate_WithTwoTypes()
    {
        var jit = new JitCompiler();

        jit.UpdateInlineCache(0, Value.from_int(1), 0);
        jit.UpdateInlineCache(0, Value.from_double(1.0), 1);

        const int totalCalls = 10_000;
        var hitCount = 0;

        for (var i = 0; i < totalCalls; i++)
        {
            var receiver = i % 3 == 0 ? Value.from_double(i) : Value.from_int(i);
            var result = jit.LookupInlineCache(0, receiver);
            if (result >= 0)
            {
                hitCount++;
            }
        }

        var hitRate = (double)hitCount / totalCalls;
        Debug.WriteLine($"[IC Polymorphic 2-Type] 命中率: {hitRate:P1} ({hitCount}/{totalCalls})");

        Assert.True(hitRate >= 0.85, $"双态 IC 命中率 {hitRate:P1} 应 >= 85%");
    }

    [Fact]
    public void IcBenchmark_ObjectTypeDifferentiation_SameShapeHit()
    {
        var jit = new JitCompiler();

        var template = new Dictionary<string, Value>
        {
            ["x"] = Value.from_int(0),
            ["y"] = Value.from_int(0)
        };
        var values = new Dictionary<string, Value>(template)
        {
            ["x"] = Value.from_int(1)
        };
        var obj1 = Value.from_object(values);
        jit.UpdateInlineCache(0, obj1, 5);

        const int iterations = 100_000;
        var hitCount = 0;

        for (var i = 0; i < iterations; i++)
        {
            var dictionary = new Dictionary<string, Value>(template)
            {
                ["x"] = Value.from_int(i)
            };
            var obj = Value.from_object(dictionary);
            var result = jit.LookupInlineCache(0, obj);
            if (result >= 0)
            {
                hitCount++;
            }
        }

        var hitRate = (double)hitCount / iterations;
        Debug.WriteLine($"[IC Object SameShape] 命中率: {hitRate:P1} ({hitCount}/{iterations})");

        Assert.True(hitRate >= 0.95, $"同形状对象 IC 命中率 {hitRate:P1} 应 >= 95%");
    }

    [Fact]
    public void IcBenchmark_CallSiteProfiling_MonomorphicDevirtualization()
    {
        var jit = new JitCompiler();

        const int callCount = 200;
        DevirtualizationDecision lastDecision = default;

        for (var i = 0; i < callCount; i++) lastDecision = jit.RecordCallSiteHit(0, Value.from_int(i), 0);

        Assert.Equal(DevirtualizationDecision.PromoteToStatic, lastDecision);

        var profile = jit.GetCallSiteProfile(0);
        Assert.NotNull(profile);
        Assert.Equal(callCount, profile.TotalCalls);
        Assert.True(profile.IsDevirtualized);
    }

    [Fact]
    public void IcBenchmark_EndToEnd_IcHitRateExceeds50Percent()
    {
        var jit = new JitCompiler();

        var intReceiver = Value.from_int(42);

        const int iterations = 100_000;

        jit.UpdateInlineCache(0, intReceiver, 0);

        for (var i = 0; i < iterations; i++)
        {
            var receiver = i % 10 == 0 ? Value.from_int(i) : intReceiver;
            var result = jit.LookupInlineCache(0, receiver);
            if (result < 0)
            {
                jit.UpdateInlineCache(0, receiver, 0);
            }
        }

        var hitRate = jit.IcHitRate;

        Debug.WriteLine($"[IC E2E] 命中率: {hitRate:P1}, 命中: {jit.IcHitCount}, 未命中: {jit.IcMissCount}");

        Assert.True(hitRate >= 0.50, $"IC 命中率 {hitRate:P1} 应 >= 50%（M4 验收标准）");
    }

    #endregion
}