namespace Nyar.Optimizer.CostModels;

/// <summary>
///     代价向量，描述编译或执行的各项资源消耗
/// </summary>
public readonly struct CostVector : IEquatable<CostVector>, IComparable<CostVector>
{
    /// <summary>
    ///     延迟 (周期数)
    /// </summary>
    public double latency { get; }

    /// <summary>
    ///     功耗 (瓦特)
    /// </summary>
    public double power { get; }

    /// <summary>
    ///     内存占用 (字节)
    /// </summary>
    public long memory { get; }

    /// <summary>
    ///     硬件面积 (门数)
    /// </summary>
    public long area { get; }

    /// <summary>
    ///     创建代价向量
    /// </summary>
    /// <param name="latency">延迟 (周期数)。</param>
    /// <param name="power">功耗 (瓦特)。</param>
    /// <param name="memory">内存占用 (字节)。</param>
    /// <param name="area">硬件面积 (门数)。</param>
    public CostVector(double latency, double power, long memory, long area)
    {
        this.latency = latency;
        this.power = power;
        this.memory = memory;
        this.area = area;
    }

    /// <summary>
    ///     全零代价向量
    /// </summary>
    public static CostVector zero => new(0, 0, 0, 0);

    /// <summary>
    ///     最大成本向量，作为初始上界使用
    /// </summary>
    public static CostVector max_value => new(double.MaxValue, double.MaxValue, long.MaxValue, long.MaxValue);

    /// <summary>
    ///     从延迟创建代价向量
    /// </summary>
    /// <param name="latency">延迟 (周期数)。</param>
    /// <returns>仅设置延迟的代价向量。</returns>
    public static CostVector from_latency(double latency)
    {
        return new CostVector(latency, 0, 0, 0);
    }

    /// <summary>
    ///     两个代价向量相加
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>各分量之和。</returns>
    public static CostVector operator +(CostVector left, CostVector right)
    {
        return new CostVector(
            left.latency + right.latency,
            left.power + right.power,
            left.memory + right.memory,
            left.area + right.area);
    }

    /// <summary>
    ///     代价向量按标量缩放
    /// </summary>
    /// <param name="vector">代价向量。</param>
    /// <param name="scalar">缩放因子。</param>
    /// <returns>各分量乘以标量。</returns>
    public static CostVector operator *(CostVector vector, double scalar)
    {
        return new CostVector(
            vector.latency * scalar,
            vector.power * scalar,
            (long)(vector.memory * scalar),
            (long)(vector.area * scalar));
    }

    /// <summary>
    ///     代价向量按标量缩放
    /// </summary>
    /// <param name="scalar">缩放因子。</param>
    /// <param name="vector">代价向量。</param>
    /// <returns>各分量乘以标量。</returns>
    public static CostVector operator *(double scalar, CostVector vector)
    {
        return vector * scalar;
    }

    /// <inheritdoc />
    public int CompareTo(CostVector other)
    {
        var cmp = latency.CompareTo(other.latency);
        if (cmp != 0) return cmp;

        cmp = power.CompareTo(other.power);
        if (cmp != 0) return cmp;

        cmp = memory.CompareTo(other.memory);
        if (cmp != 0) return cmp;

        return area.CompareTo(other.area);
    }

    /// <inheritdoc />
    public bool Equals(CostVector other)
    {
        return latency == other.latency
               && power == other.power
               && memory == other.memory
               && area == other.area;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is CostVector other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(latency, power, memory, area);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Latency={latency}, Power={power}, Memory={memory}, Area={area}";
    }

    /// <summary>
    ///     判断两个代价向量是否相等
    /// </summary>
    public static bool operator ==(CostVector left, CostVector right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     判断两个代价向量是否不相等
    /// </summary>
    public static bool operator !=(CostVector left, CostVector right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    ///     比较两个代价向量
    /// </summary>
    public static bool operator <(CostVector left, CostVector right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    ///     比较两个代价向量
    /// </summary>
    public static bool operator <=(CostVector left, CostVector right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    ///     比较两个代价向量
    /// </summary>
    public static bool operator >(CostVector left, CostVector right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    ///     比较两个代价向量
    /// </summary>
    public static bool operator >=(CostVector left, CostVector right)
    {
        return left.CompareTo(right) >= 0;
    }
}