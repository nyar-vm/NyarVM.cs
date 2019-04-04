namespace Nyar.Tests.Memory;

public class NyarHeapTests
{
    [Fact]
    public void GlobalVariables_StoreAndLoad()
    {
        var heap = new NyarHeap();

        heap.StoreGlobal(0, Value.from_int(42));
        heap.StoreGlobal(1, Value.from_double(3.14));

        Assert.Equal(42, heap.LoadGlobal(0).@int);
        Assert.Equal(3.14, heap.LoadGlobal(1).@double);
    }

    [Fact]
    public void GlobalVariables_Nonexistent_ReturnsNull()
    {
        var heap = new NyarHeap();
        Assert.Equal(ValueType.@null, heap.LoadGlobal(999).type);
    }

    [Fact]
    public void GlobalVariables_Overwrite()
    {
        var heap = new NyarHeap();
        heap.StoreGlobal(0, Value.from_int(1));
        heap.StoreGlobal(0, Value.from_int(2));

        Assert.Equal(2, heap.LoadGlobal(0).@int);
    }

    [Fact]
    public void GetAllGlobals_ReturnsAll()
    {
        var heap = new NyarHeap();
        heap.StoreGlobal(0, Value.from_int(10));
        heap.StoreGlobal(5, Value.from_int(50));

        var globals = heap.GetAllGlobals().ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.Equal(2, globals.Count);
        Assert.Equal(10, globals[0].@int);
        Assert.Equal(50, globals[5].@int);
    }

    [Fact]
    public void MemoryAlloc_ReturnsAddress()
    {
        var heap = new NyarHeap();
        var addr1 = heap.Alloc(100);
        var addr2 = heap.Alloc(200);

        Assert.True(addr2 > addr1);
        Assert.Equal(addr1 + 100, addr2);
    }

    [Fact]
    public void MemoryAlloc_InvalidSize_Throws()
    {
        var heap = new NyarHeap();
        Assert.Throws<NyarRuntimeException>(() => heap.Alloc(0));
        Assert.Throws<NyarRuntimeException>(() => heap.Alloc(-1));
    }

    [Fact]
    public void MemoryI32_StoreAndLoad()
    {
        var heap = new NyarHeap();
        var addr = heap.Alloc(16);

        heap.StoreI32(addr, 12345);
        Assert.Equal(12345, heap.LoadI32(addr));
    }

    [Fact]
    public void MemoryI64_StoreAndLoad()
    {
        var heap = new NyarHeap();
        var addr = heap.Alloc(16);

        heap.StoreI64(addr, 9876543210L);
        Assert.Equal(9876543210L, heap.LoadI64(addr));
    }

    [Fact]
    public void MemoryI32_OutOfBounds_Throws()
    {
        var heap = new NyarHeap();
        Assert.Throws<NyarRuntimeException>(() => heap.LoadI32(-1));
        Assert.Throws<NyarRuntimeException>(() => heap.StoreI32(-1, 0));
    }

    [Fact]
    public void MemoryAutoExpand_LargeAllocation()
    {
        var heap = new NyarHeap(256);
        var addr = heap.Alloc(1024);

        heap.StoreI32(addr, 42);
        Assert.Equal(42, heap.LoadI32(addr));
    }

    [Fact]
    public void Free_RemovesAllocation()
    {
        var heap = new NyarHeap();
        var addr = heap.Alloc(64);

        heap.Free(addr);
    }

    [Fact]
    public void MultipleAllocations_SequentialAddresses()
    {
        var heap = new NyarHeap();
        var addresses = new List<int>();

        for (var i = 0; i < 10; i++) addresses.Add(heap.Alloc(32));

        for (var i = 1; i < addresses.Count; i++) Assert.Equal(addresses[i - 1] + 32, addresses[i]);
    }
}