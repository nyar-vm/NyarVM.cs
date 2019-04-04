namespace Core.Memory;

/// <summary>
///     内存池接口，提供内存的租用与归还
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public interface IMemoryPool<T>
{
    /// <summary>
    ///     租用指定大小的内存
    /// </summary>
    /// <param name="size">请求的元素数量</param>
    /// <returns>内存所有者实例</returns>
    IMemoryOwner<T> rent(int size);

    /// <summary>
    ///     归还内存到池中
    /// </summary>
    /// <param name="owner">要归还的内存所有者</param>
    void @return(IMemoryOwner<T> owner);
}