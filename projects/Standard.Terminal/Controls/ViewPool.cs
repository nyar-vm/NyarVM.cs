using System.Collections.Concurrent;

namespace Std.Terminal.Controls;

/// <summary>
///     View 对象池，复用控件实例以降低 GC 压力
/// </summary>
/// <typeparam name="T">View 子类型</typeparam>
public sealed class ViewPool<T> where T : View, new()
{
    private readonly Func<T>? _factory;
    private readonly int _max_pool_size;
    private readonly ConcurrentBag<T> _pool = [];

    /// <summary>
    ///     创建 View 池
    /// </summary>
    /// <param name="factory">自定义工厂方法</param>
    /// <param name="maxPoolSize">最大池大小</param>
    public ViewPool(Func<T>? factory = null, int maxPoolSize = 64)
    {
        _factory = factory;
        _max_pool_size = maxPoolSize;
    }

    /// <summary>
    ///     当前池中可用实例数
    /// </summary>
    public int available_count => _pool.Count;

    /// <summary>
    ///     从池中获取一个实例
    /// </summary>
    public T rent()
    {
        if (_pool.TryTake(out var view)) return view;

        return _factory != null ? _factory() : new T();
    }

    /// <summary>
    ///     归还实例到池中
    /// </summary>
    /// <param name="view">要归还的 View</param>
    public void @return(T view)
    {
        if (_pool.Count >= _max_pool_size) return;

        view.visible = false;
        view.tab_stop = false;
        _pool.Add(view);
    }

    /// <summary>
    ///     清空池
    /// </summary>
    public void clear()
    {
        while (_pool.TryTake(out _))
        {
        }
    }
}