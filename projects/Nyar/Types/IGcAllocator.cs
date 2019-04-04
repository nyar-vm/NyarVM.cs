namespace Nyar.Types;

/// <summary>
///     GC 分配器接口，供 Value 工厂方法委托对象分配
/// </summary>
public interface IGcAllocator
{
    /// <summary>
    ///     分配对象表槽位，返回索引
    /// </summary>
    /// <param name="obj">要存储的对象。</param>
    /// <returns>对象表索引。</returns>
    int allocate(object obj);
}