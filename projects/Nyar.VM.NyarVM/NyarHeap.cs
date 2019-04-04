using Nyar.Types;

namespace Nyar.VM.NyarVM;

/// <summary>
///     Nyar 虚拟机堆内存，管理全局变量和线性内存
/// </summary>
public sealed class NyarHeap
{
    /// <summary>
    ///     已分配的内存块（地址 → 大小）
    /// </summary>
    private readonly Dictionary<int, int> _allocated_blocks;

    /// <summary>
    ///     全局变量存储
    /// </summary>
    private readonly Dictionary<int, Value> _globals;

    /// <summary>
    ///     线性内存块
    /// </summary>
    private byte[] _memory;

    /// <summary>
    ///     下一个分配地址
    /// </summary>
    private int _next_address;

    /// <summary>
    ///     初始化 NyarHeap
    /// </summary>
    /// <param name="memorySize">线性内存初始大小。</param>
    public NyarHeap(int memorySize = 64 * 1024)
    {
        _globals = new Dictionary<int, Value>();
        _memory = new byte[memorySize];
        _allocated_blocks = new Dictionary<int, int>();
        _next_address = 256;
    }

    /// <summary>
    ///     获取所有全局变量（用于 GC Root 扫描）
    /// </summary>
    /// <returns>全局变量的键值对枚举。</returns>
    public IEnumerable<KeyValuePair<int, Value>> get_all_globals()
    {
        return _globals;
    }

    /// <summary>
    ///     确保线性内存容量足够
    /// </summary>
    /// <param name="requiredSize">所需最小大小。</param>
    private void ensure_capacity(int requiredSize)
    {
        if (requiredSize <= _memory.Length) return;

        var newSize = _memory.Length;
        while (newSize < requiredSize) newSize *= 2;

        Array.Resize(ref _memory, newSize);
    }

    #region 全局变量

    /// <summary>
    ///     加载全局变量
    /// </summary>
    /// <param name="index">全局变量索引。</param>
    /// <returns>全局变量值。</returns>
    public Value load_global(int index)
    {
        return _globals.GetValueOrDefault(index, Value.@null);
    }

    /// <summary>
    ///     存储全局变量
    /// </summary>
    /// <param name="index">全局变量索引。</param>
    /// <param name="value">要存储的值。</param>
    public void store_global(int index, Value value)
    {
        _globals[index] = value;
    }

    #endregion

    #region 内存分配

    /// <summary>
    ///     分配内存块
    /// </summary>
    /// <param name="size">请求的字节数。</param>
    /// <returns>分配的起始地址。</returns>
    public int alloc(int size)
    {
        if (size <= 0) throw new NyarRuntimeException($"无效的分配大小: {size}");

        var address = _next_address;
        ensure_capacity(address + size);
        _allocated_blocks[address] = size;
        _next_address = address + size;
        return address;
    }

    /// <summary>
    ///     释放内存块
    /// </summary>
    /// <param name="address">要释放的起始地址。</param>
    public void free(int address)
    {
        _allocated_blocks.Remove(address);
    }

    #endregion

    #region 内存读写

    /// <summary>
    ///     从线性内存加载 i32
    /// </summary>
    /// <param name="address">内存地址。</param>
    /// <returns>i32 值。</returns>
    public int load_i32(int address)
    {
        if (address < 0 || address + 4 > _memory.Length) throw new NyarRuntimeException($"内存读取越界: 地址 {address}");

        return BitConverter.ToInt32(_memory, address);
    }

    /// <summary>
    ///     向线性内存存储 i32
    /// </summary>
    /// <param name="address">内存地址。</param>
    /// <param name="value">要存储的值。</param>
    public void store_i32(int address, int value)
    {
        if (address < 0 || address + 4 > _memory.Length) throw new NyarRuntimeException($"内存写入越界: 地址 {address}");

        BitConverter.TryWriteBytes(_memory.AsSpan(address, 4), value);
    }

    /// <summary>
    ///     从线性内存加载 i64
    /// </summary>
    /// <param name="address">内存地址。</param>
    /// <returns>i64 值。</returns>
    public long load_i64(int address)
    {
        if (address < 0 || address + 8 > _memory.Length) throw new NyarRuntimeException($"内存读取越界: 地址 {address}");

        return BitConverter.ToInt64(_memory, address);
    }

    /// <summary>
    ///     向线性内存存储 i64
    /// </summary>
    /// <param name="address">内存地址。</param>
    /// <param name="value">要存储的值。</param>
    public void store_i64(int address, long value)
    {
        if (address < 0 || address + 8 > _memory.Length) throw new NyarRuntimeException($"内存写入越界: 地址 {address}");

        BitConverter.TryWriteBytes(_memory.AsSpan(address, 8), value);
    }

    #endregion
}