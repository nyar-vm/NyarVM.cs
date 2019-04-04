namespace Std;

/// <summary>
///     集合工厂方法，用于创建带有比较器或初始容量的集合
/// </summary>
public static class CollectionFactory
{
    /// <summary>
    ///     创建带有指定比较器的 SortedSet
    /// </summary>
    public static SortedSet<T> with<T>(IComparer<T> comparer)
    {
        return new SortedSet<T>(comparer);
    }

    /// <summary>
    ///     创建带有指定比较器的 HashSet
    /// </summary>
    public static HashSet<T> with<T>(IEqualityComparer<T> comparer)
    {
        return new HashSet<T>(comparer);
    }

    /// <summary>
    ///     创建带有指定初始容量的 List
    /// </summary>
    public static List<T> with<T>(int capacity)
    {
        return new List<T>(capacity);
    }
}