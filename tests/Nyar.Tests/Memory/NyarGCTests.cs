using System.Collections;

namespace Nyar.Tests.Memory;

public class NyarGcTests
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

    #region 引用跟踪

    [Fact]
    public void IsReferenceType_ObjectTag_ReturnsTrue()
    {
        Assert.True(NyarGC.IsReferenceType(Value.from_object("test")));
    }

    [Fact]
    public void IsReferenceType_IntTag_ReturnsFalse()
    {
        Assert.False(NyarGC.IsReferenceType(Value.from_int(42)));
    }

    [Fact]
    public void IsReferenceType_BoolTag_ReturnsFalse()
    {
        Assert.False(NyarGC.IsReferenceType(Value.from_bool(true)));
    }

    [Fact]
    public void IsReferenceType_NullTag_ReturnsFalse()
    {
        Assert.False(NyarGC.IsReferenceType(Value.@null));
    }

    [Fact]
    public void IsReferenceType_StringTag_ReturnsTrue()
    {
        Assert.True(NyarGC.IsReferenceType(Value.from_string("hello")));
    }

    [Fact]
    public void IsReferenceType_BigIntTag_ReturnsTrue()
    {
        Assert.True(NyarGC.IsReferenceType(Value.from_big_int(999)));
    }

    [Fact]
    public void IsReferenceType_ClosureTag_ReturnsTrue()
    {
        var func = new NyarFunction("test", 0, 0, 0, 1);
        var closure = new NyarClosure(func, []);
        Assert.True(NyarGC.IsReferenceType(Value.from_closure(closure)));
    }

    #endregion

    #region Value 对象表集成

    [Fact]
    public void FromObject_CreatesReferenceType()
    {
        var v = Value.from_object("hello");
        Assert.True(NyarGC.IsReferenceType(v));
    }

    [Fact]
    public void FromString_CreatesReferenceType()
    {
        var v = Value.from_string("test");
        Assert.True(NyarGC.IsReferenceType(v));
    }

    [Fact]
    public void Object_RoundTrip()
    {
        var obj = new List<int> { 1, 2, 3 };
        var v = Value.from_object(obj);
        var retrieved = v.@object as List<int>;
        Assert.NotNull(retrieved);
        Assert.Equal([1, 2, 3], retrieved);
    }

    [Fact]
    public void String_RoundTrip()
    {
        var v = Value.from_string("test_string");
        Assert.Equal("test_string", v.@string);
    }

    #endregion

    #region 子对象提取（通过 GC 行为间接测试）

    [Fact]
    public void Closure_WithUpvalues_StoredCorrectly()
    {
        var upvalue1 = Value.from_int(10);
        var upvalue2 = Value.from_int(20);
        var func = new NyarFunction("test", 0, 0, 0, 1);
        var closure = new NyarClosure(func, [upvalue1, upvalue2]);
        var v = Value.from_closure(closure);

        Assert.True(NyarGC.IsReferenceType(v));
        var retrieved = v.closure as NyarClosure;
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.Upvalues.Count);
        Assert.Equal(10, retrieved.Upvalues[0].@int);
        Assert.Equal(20, retrieved.Upvalues[1].@int);
    }

    [Fact]
    public void Dictionary_WithChildValues_StoredCorrectly()
    {
        var childValue = Value.from_int(42);
        var dict = new Hashtable
        {
            ["key"] = childValue
        };
        var v = Value.from_object(dict);

        Assert.True(NyarGC.IsReferenceType(v));
        var retrieved = v.@object as Hashtable;
        Assert.NotNull(retrieved);
        Assert.True(retrieved.ContainsKey("key"));
    }

    #endregion

    #region 压力测试

    [Fact]
    public void StressTest_ManyObjectCreations()
    {
        var values = new List<Value>();
        for (var i = 0; i < 1000; i++) values.Add(Value.from_object($"stress_{i}"));

        Assert.Equal(1000, values.Count);
        Assert.All(values, v => Assert.True(NyarGC.IsReferenceType(v)));
    }

    [Fact]
    public void StressTest_ManyStringCreations()
    {
        var values = new List<Value>();
        for (var i = 0; i < 1000; i++) values.Add(Value.from_string($"str_{i}"));

        Assert.Equal(1000, values.Count);
        Assert.All(values, v => Assert.True(NyarGC.IsReferenceType(v)));
    }

    [Fact]
    public void StressTest_CircularReferences()
    {
        var dict1 = new Hashtable();
        var dict2 = new Hashtable();
        dict1["ref"] = dict2;
        dict2["ref"] = dict1;

        var v1 = Value.from_object(dict1);
        var v2 = Value.from_object(dict2);

        Assert.True(NyarGC.IsReferenceType(v1));
        Assert.True(NyarGC.IsReferenceType(v2));
    }

    [Fact]
    public void StressTest_MixedTypeCreations()
    {
        for (var i = 0; i < 500; i++)
        {
            Value.from_int(i);
            Value.from_bool(i % 2 == 0);
            _ = Value.@null;
            Value.from_object($"obj_{i}");
            Value.from_string($"str_{i}");
        }
    }

    #endregion
}