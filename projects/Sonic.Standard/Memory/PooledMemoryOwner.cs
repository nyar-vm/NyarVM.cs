using Core.Memory;

namespace Std.Memory;

/// <summary>
///     池化内存所有者，实现 IMemoryOwner&lt;T&gt; 接口，管理从池中租用的内存
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public sealed class PooledMemoryOwner<T> : IMemoryOwner<T>
{
    /// <summary>
    ///     底层数组
    /// </summary>
    private readonly T[] _array;

    /// <summary>
    ///     池引用
    /// </summary>
    private readonly ArrayPool<T> _pool;

    /// <summary>
    ///     是否已释放
    /// </summary>
    private bool _disposed;

    /// <summary>
    ///     初始化池化内存所有者
    /// </summary>
    /// <param name="array">租用的数组</param>
    /// <param name="pool">来源池</param>
    public PooledMemoryOwner(T[] array, ArrayPool<T> pool)
    {
        _array = array;
        _pool = pool;
    }

    /// <summary>
    ///     获取内存跨度
    /// </summary>
    public Span<T> span =>
        _disposed ? throw new ObjectDisposedException(nameof(PooledMemoryOwner<T>)) : new Span<T>(_array);

    /// <summary>
    ///     释放内存，归还到池
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _pool.return_array(_array);
            _disposed = true;
        }
    }
}