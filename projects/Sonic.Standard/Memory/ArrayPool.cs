using Core.Memory;

namespace Std.Memory;

/// <summary>
///     对 `System.Buffers.ArrayPool` 的轻量包装，保留 `Sonic.Standard` 现有的小写 API 习惯。
/// </summary>
/// <typeparam name="T">数组元素类型。</typeparam>
public sealed class ArrayPool<T> : IMemoryPool<T>
{
    private readonly System.Buffers.ArrayPool<T> _pool;

    private ArrayPool(System.Buffers.ArrayPool<T> pool)
    {
        _pool = pool;
    }

    /// <summary>
    ///     获取共享池实例
    /// </summary>
    public static ArrayPool<T> shared { get; } = new(System.Buffers.ArrayPool<T>.Shared);

    /// <summary>
    ///     租用指定大小的内存
    /// </summary>
    /// <param name="size">请求的元素数量</param>
    /// <returns>内存所有者实例</returns>
    public IMemoryOwner<T> rent(int size)
    {
        return new PooledMemoryOwner<T>(rent_array(size), this);
    }

    /// <summary>
    ///     归还内存到池中
    /// </summary>
    /// <param name="owner">要归还的内存所有者</param>
    public void @return(IMemoryOwner<T> owner)
    {
        if (owner is PooledMemoryOwner<T> pooled) pooled.Dispose();
    }

    /// <summary>
    ///     租用指定长度的数组
    /// </summary>
    /// <param name="minimumLength">请求的最小长度</param>
    /// <returns>租用的数组</returns>
    public T[] rent_array(int minimumLength)
    {
        return _pool.Rent(minimumLength);
    }

    /// <summary>
    ///     归还数组到池中
    /// </summary>
    /// <param name="array">要归还的数组</param>
    /// <param name="clearArray">是否清除数组内容</param>
    public void return_array(T[] array, bool clearArray = false)
    {
        _pool.Return(array, clearArray);
    }
}