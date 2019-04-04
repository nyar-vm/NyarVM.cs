using System.Collections.Concurrent;

namespace Std.DL.Flux;

/// <summary>
///     张量对象池 —— 复用 float[] 数组以减少 GC 压力。
///     训练循环中大量临时张量（中间激活、梯度）频繁分配和释放，
///     通过池化复用底层 float[] 数组，避免重复分配。
/// </summary>
public sealed class ArrayNDPool
{
    private readonly ConcurrentDictionary<int, ConcurrentBag<float[]>> _buckets = new();
    private int _totalAllocated;
    private int _totalRented;
    private int _totalReturned;

    /// <summary>
    ///     已租出的数组数量
    /// </summary>
    public int TotalRented => _totalRented;

    /// <summary>
    ///     已归还的数组数量
    /// </summary>
    public int TotalReturned => _totalReturned;

    /// <summary>
    ///     新分配的数组数量（池未命中）
    /// </summary>
    public int TotalAllocated => _totalAllocated;

    /// <summary>
    ///     池命中率
    /// </summary>
    public double HitRate => _totalRented > 0 ? (double)(_totalRented - _totalAllocated) / _totalRented : 0;

    /// <summary>
    ///     从池中租用一个 float[] 数组
    /// </summary>
    /// <param name="length">所需数组长度</param>
    /// <returns>可用的 float[] 数组（内容未清零）</returns>
    public float[] Rent(int length)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), "数组长度必须大于 0");

        var bucketKey = GetBucketKey(length);

        if (_buckets.TryGetValue(bucketKey, out var bucket) && bucket.TryTake(out var array))
        {
            Interlocked.Increment(ref _totalRented);
            return array;
        }

        Interlocked.Increment(ref _totalRented);
        Interlocked.Increment(ref _totalAllocated);
        return new float[length];
    }

    /// <summary>
    ///     将 float[] 数组归还到池中
    /// </summary>
    /// <param name="array">要归还的数组</param>
    public void Return(float[] array)
    {
        if (array is null) return;

        var bucketKey = GetBucketKey(array.Length);
        var bucket = _buckets.GetOrAdd(bucketKey, _ => []);
        bucket.Add(array);
        Interlocked.Increment(ref _totalReturned);
    }

    /// <summary>
    ///     从池中租用数组并创建全零张量
    /// </summary>
    /// <param name="shape">张量形状</param>
    /// <returns>全零张量（底层数组可能来自池）</returns>
    public ArrayND RentZeros(params int[] shape)
    {
        var size = shape.Aggregate(1, (a, b) => a * b);
        var data = Rent(size);
        Array.Clear(data, 0, size);
        return ArrayND.FromArray(data, shape);
    }

    /// <summary>
    ///     归还张量的底层数组到池中（仅当张量独占该数组时有效）
    /// </summary>
    /// <param name="tensor">要回收的张量</param>
    public void ReturnTensor(ArrayND tensor)
    {
        if (tensor is null) return;

        var data = tensor.TryGetOwnedData();
        if (data is not null) Return(data);
    }

    /// <summary>
    ///     清空池中所有缓存的数组
    /// </summary>
    public void Clear()
    {
        _buckets.Clear();
    }

    /// <summary>
    ///     获取池的统计信息
    /// </summary>
    public PoolStats GetStats()
    {
        var bucketCount = 0;
        var totalCachedArrays = 0;
        var totalCachedElements = 0L;

        foreach (var kv in _buckets)
        {
            bucketCount++;
            var count = kv.Value.Count;
            totalCachedArrays += count;
            totalCachedElements += (long)kv.Key * count;
        }

        return new PoolStats(
            bucketCount,
            totalCachedArrays,
            totalCachedElements,
            _totalRented,
            _totalReturned,
            _totalAllocated,
            HitRate
        );
    }

    /// <summary>
    ///     计算桶键（向上取整到最近的 2 的幂，减少桶数量）
    /// </summary>
    private static int GetBucketKey(int length)
    {
        var key = 1;
        while (key < length) key <<= 1;

        return key;
    }

    /// <summary>
    ///     池统计信息
    /// </summary>
    public readonly record struct PoolStats(
        int BucketCount,
        int TotalCachedArrays,
        long TotalCachedElements,
        int TotalRented,
        int TotalReturned,
        int TotalAllocated,
        double HitRate
    );
}