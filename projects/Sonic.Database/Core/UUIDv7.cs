namespace Olympus.Athena.Core;

#region UUIDv7 唯一标识符

/// <summary>
///     UUIDv7 唯一标识符，基于 Unix 毫秒时间戳的单调递增有序 UUID
/// </summary>
public readonly struct UUIDv7 : IComparable<UUIDv7>, IEquatable<UUIDv7>
{
    #region 静态字段

    private static long _counter;
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    #endregion

    #region 存储

    /// <summary>
    ///     高位 64 位：48 位 Unix 毫秒时间戳 + 4 位版本号(0x7) + 12 位计数器序列
    /// </summary>
    public ulong High { get; }

    /// <summary>
    ///     低位 64 位：2 位变体(10) + 62 位随机位
    /// </summary>
    public ulong Low { get; }

    #endregion

    #region 构造函数

    /// <summary>
    ///     从原始高位和低位构造 UUIDv7
    /// </summary>
    /// <param name="high">高位 64 位</param>
    /// <param name="low">低位 64 位</param>
    public UUIDv7(ulong high, ulong low)
    {
        High = high;
        Low = low;
    }

    #endregion

    #region 工厂方法

    /// <summary>
    ///     生成新的 UUIDv7，使用当前 UTC 毫秒时间戳和递增序列号
    /// </summary>
    /// <returns>新的 UUIDv7 实例</returns>
    public static UUIDv7 New()
    {
        while (true)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var lastCounter = _counter;
            var lastTimestamp = lastCounter >> 14;
            var lastSequence = (int)(lastCounter & 0x3FFF);

            if (timestamp == lastTimestamp)
            {
                var newSequence = lastSequence + 1;
                if (newSequence > 0xFFF)
                {
                    Thread.SpinWait(100);
                    continue;
                }

                var newCounter = (timestamp << 14) | (uint)newSequence;
                if (Interlocked.CompareExchange(ref _counter, newCounter, lastCounter) == lastCounter)
                    return CreateFromTimestampAndSequence(timestamp, newSequence);
            }
            else
            {
                var newCounter = timestamp << 14;
                if (Interlocked.CompareExchange(ref _counter, newCounter, lastCounter) == lastCounter)
                    return CreateFromTimestampAndSequence(timestamp, 0);
            }
        }
    }

    private static UUIDv7 CreateFromTimestampAndSequence(long timestampMs, int sequence)
    {
        Span<byte> randomBytes = stackalloc byte[10];
        Rng.GetBytes(randomBytes);

        var randA = (randomBytes[0] << 4) | (randomBytes[1] >> 4);

        var randB = ((ulong)(randomBytes[1] & 0xF) << 58)
                    | ((ulong)randomBytes[2] << 50)
                    | ((ulong)randomBytes[3] << 42)
                    | ((ulong)randomBytes[4] << 34)
                    | ((ulong)randomBytes[5] << 26)
                    | ((ulong)randomBytes[6] << 18)
                    | ((ulong)randomBytes[7] << 10)
                    | ((ulong)randomBytes[8] << 2)
                    | ((ulong)randomBytes[9] >> 6);

        var high = ((ulong)timestampMs << 16) | (0x7UL << 12) | ((ulong)randA & 0xFFF);
        var low = (0x2UL << 62) | (randB & 0x3FFFFFFFFFFFFFFFUL);

        return new UUIDv7(high, low);
    }

    /// <summary>
    ///     从 <see cref="Guid" /> 构造 UUIDv7
    /// </summary>
    /// <param name="guid">System.Guid 实例</param>
    /// <returns>对应的 UUIDv7 实例</returns>
    public static UUIDv7 FromGuid(Guid guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);

        var high = ((ulong)bytes[3] << 56)
                   | ((ulong)bytes[2] << 48)
                   | ((ulong)bytes[1] << 40)
                   | ((ulong)bytes[0] << 32)
                   | ((ulong)bytes[5] << 24)
                   | ((ulong)bytes[4] << 16)
                   | ((ulong)bytes[7] << 8)
                   | bytes[6];

        var low = ((ulong)bytes[8] << 56)
                  | ((ulong)bytes[9] << 48)
                  | ((ulong)bytes[10] << 40)
                  | ((ulong)bytes[11] << 32)
                  | ((ulong)bytes[12] << 24)
                  | ((ulong)bytes[13] << 16)
                  | ((ulong)bytes[14] << 8)
                  | bytes[15];

        return new UUIDv7(high, low);
    }

    #endregion

    #region 转换方法

    /// <summary>
    ///     转换为 <see cref="Guid" />
    /// </summary>
    /// <returns>对应的 Guid 实例</returns>
    public Guid ToGuid()
    {
        Span<byte> bytes = stackalloc byte[16];

        bytes[0] = (byte)(High >> 56);
        bytes[1] = (byte)(High >> 48);
        bytes[2] = (byte)(High >> 40);
        bytes[3] = (byte)(High >> 32);
        bytes[4] = (byte)(High >> 24);
        bytes[5] = (byte)(High >> 16);
        bytes[6] = (byte)(High >> 8);
        bytes[7] = (byte)High;

        bytes[8] = (byte)(Low >> 56);
        bytes[9] = (byte)(Low >> 48);
        bytes[10] = (byte)(Low >> 40);
        bytes[11] = (byte)(Low >> 32);
        bytes[12] = (byte)(Low >> 24);
        bytes[13] = (byte)(Low >> 16);
        bytes[14] = (byte)(Low >> 8);
        bytes[15] = (byte)Low;

        var a = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
        var b = (short)((bytes[4] << 8) | bytes[5]);
        var c = (short)((bytes[6] << 8) | bytes[7]);

        return new Guid(a, b, c, bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15]);
    }

    #endregion

    #region 时间戳提取

    /// <summary>
    ///     提取 Unix 毫秒时间戳
    /// </summary>
    public long TimestampMs => (long)(High >> 16);

    /// <summary>
    ///     提取 <see cref="DateTimeOffset" /> 时间戳
    /// </summary>
    public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(TimestampMs);

    #endregion

    #region 格式化

    /// <summary>
    ///     返回标准 UUID 格式字符串（8-4-4-4-12）
    /// </summary>
    public override string ToString()
    {
        return ToGuid().ToString();
    }

    #endregion

    #region 相等性

    /// <inheritdoc />
    public bool Equals(UUIDv7 other)
    {
        return High == other.High && Low == other.Low;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is UUIDv7 other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(High, Low);
    }

    /// <summary>
    ///     比较两个 UUIDv7 是否相等
    /// </summary>
    public static bool operator ==(UUIDv7 left, UUIDv7 right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     比较两个 UUIDv7 是否不相等
    /// </summary>
    public static bool operator !=(UUIDv7 left, UUIDv7 right)
    {
        return !left.Equals(right);
    }

    #endregion

    #region 比较与排序

    /// <inheritdoc />
    public int CompareTo(UUIDv7 other)
    {
        var timestampCompare = TimestampMs.CompareTo(other.TimestampMs);
        if (timestampCompare != 0) return timestampCompare;

        var highCompare = High.CompareTo(other.High);
        if (highCompare != 0) return highCompare;

        return Low.CompareTo(other.Low);
    }

    /// <summary>
    ///     小于比较
    /// </summary>
    public static bool operator <(UUIDv7 left, UUIDv7 right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    ///     大于比较
    /// </summary>
    public static bool operator >(UUIDv7 left, UUIDv7 right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    ///     小于等于比较
    /// </summary>
    public static bool operator <=(UUIDv7 left, UUIDv7 right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    ///     大于等于比较
    /// </summary>
    public static bool operator >=(UUIDv7 left, UUIDv7 right)
    {
        return left.CompareTo(right) >= 0;
    }

    #endregion
}

#endregion