using System;

namespace Core.Memory;

/// <summary>
///     内存所有者接口，管理一段内存的生命周期
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public interface IMemoryOwner<T> : IDisposable
{
    /// <summary>
    ///     获取所拥有的内存跨度
    /// </summary>
    Span<T> span { get; }
}