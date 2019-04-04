using System.Collections.Concurrent;

namespace Std.Terminal.Controls;

/// <summary>
///     全局 View 池管理器
/// </summary>
public static class ViewPoolManager
{
    private static readonly ConcurrentDictionary<Type, object> _pools = new();

    /// <summary>
    ///     获取或创建指定类型的 View 池
    /// </summary>
    /// <typeparam name="T">View 类型</typeparam>
    /// <param name="maxPoolSize">最大池大小</param>
    public static ViewPool<T> get_pool<T>(int maxPoolSize = 64) where T : View, new()
    {
        var pool = _pools.GetOrAdd(typeof(T), _ =>
            new ViewPool<T>(maxPoolSize: maxPoolSize));
        return (ViewPool<T>)pool;
    }

    /// <summary>
    ///     从池中借用 View
    /// </summary>
    /// <typeparam name="T">View 类型</typeparam>
    public static T rent<T>() where T : View, new()
    {
        return get_pool<T>().rent();
    }

    /// <summary>
    ///     归还 View 到池中
    /// </summary>
    /// <typeparam name="T">View 类型</typeparam>
    /// <param name="view">要归还的 View</param>
    public static void @return<T>(T view) where T : View, new()
    {
        get_pool<T>().@return(view);
    }

    /// <summary>
    ///     清空所有池
    /// </summary>
    public static void clear_all()
    {
        foreach (var kv in _pools)
        {
            var clearMethod = kv.Value.GetType().GetMethod("Clear");
            clearMethod?.Invoke(kv.Value, null);
        }

        _pools.Clear();
    }
}