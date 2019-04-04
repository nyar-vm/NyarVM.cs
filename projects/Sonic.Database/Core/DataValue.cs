namespace Olympus.Athena.Core;

#region DataValue 统一值类型

/// <summary>
///     Athena 统一值类型，内部使用判别联合表示不同的数据类型
/// </summary>
public readonly struct DataValue : IEquatable<DataValue>
{
    #region 存储槽

    private readonly long _int64;
    private readonly double _float64;
    private readonly string? _string;
    private readonly byte[]? _bytes;
    private readonly Guid _guid;
    private readonly bool _bool;

    #endregion

    #region 构造函数

    private DataValue(DataType type, long int64 = 0, double float64 = 0, string? str = null, byte[]? bytes = null,
        Guid guid = default, bool b = false)
    {
        Kind = type;
        _int64 = int64;
        _float64 = float64;
        _string = str;
        _bytes = bytes;
        _guid = guid;
        _bool = b;
    }

    #endregion

    #region 属性

    /// <summary>
    ///     获取当前存储的数据类型
    /// </summary>
    public DataType Kind { get; }

    /// <summary>
    ///     是否为 Null 值
    /// </summary>
    public bool IsNull => Kind == DataType.Null;

    #endregion

    #region 类型转换方法

    /// <summary>
    ///     以 Int64 类型获取值，类型不匹配时抛出 <see cref="InvalidCastException" />
    /// </summary>
    public long AsInt64()
    {
        if (Kind != DataType.Int64) throw new InvalidCastException($"无法将 {Kind} 类型转换为 Int64");

        return _int64;
    }

    /// <summary>
    ///     以 Float64 类型获取值，类型不匹配时抛出 <see cref="InvalidCastException" />
    /// </summary>
    public double AsFloat64()
    {
        if (Kind != DataType.Float64) throw new InvalidCastException($"无法将 {Kind} 类型转换为 Float64");

        return _float64;
    }

    /// <summary>
    ///     以 String 类型获取值，类型不匹配时抛出 <see cref="InvalidCastException" />
    /// </summary>
    public string AsString()
    {
        if (Kind != DataType.String) throw new InvalidCastException($"无法将 {Kind} 类型转换为 String");

        return _string!;
    }

    /// <summary>
    ///     以 Bytes 类型获取值，类型不匹配时抛出 <see cref="InvalidCastException" />
    /// </summary>
    public byte[] AsBytes()
    {
        if (Kind != DataType.Bytes) throw new InvalidCastException($"无法将 {Kind} 类型转换为 Bytes");

        return _bytes!;
    }

    /// <summary>
    ///     以 Guid 类型获取值，类型不匹配时抛出 <see cref="InvalidCastException" />
    /// </summary>
    public Guid AsGuid()
    {
        if (Kind != DataType.Guid) throw new InvalidCastException($"无法将 {Kind} 类型转换为 Guid");

        return _guid;
    }

    /// <summary>
    ///     以 Bool 类型获取值，类型不匹配时抛出 <see cref="InvalidCastException" />
    /// </summary>
    public bool AsBool()
    {
        if (Kind != DataType.Bool) throw new InvalidCastException($"无法将 {Kind} 类型转换为 Bool");

        return _bool;
    }

    #endregion

    #region 安全获取方法

    /// <summary>
    ///     尝试获取 Int64 值
    /// </summary>
    /// <param name="value">输出值</param>
    /// <returns>类型匹配时返回 <c>true</c></returns>
    public bool TryGetInt64(out long value)
    {
        if (Kind == DataType.Int64)
        {
            value = _int64;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     尝试获取 Float64 值
    /// </summary>
    /// <param name="value">输出值</param>
    /// <returns>类型匹配时返回 <c>true</c></returns>
    public bool TryGetFloat64(out double value)
    {
        if (Kind == DataType.Float64)
        {
            value = _float64;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     尝试获取 String 值
    /// </summary>
    /// <param name="value">输出值</param>
    /// <returns>类型匹配时返回 <c>true</c></returns>
    public bool TryGetString(out string? value)
    {
        if (Kind == DataType.String)
        {
            value = _string;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     尝试获取 Bytes 值
    /// </summary>
    /// <param name="value">输出值</param>
    /// <returns>类型匹配时返回 <c>true</c></returns>
    public bool TryGetBytes(out byte[]? value)
    {
        if (Kind == DataType.Bytes)
        {
            value = _bytes;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     尝试获取 Guid 值
    /// </summary>
    /// <param name="value">输出值</param>
    /// <returns>类型匹配时返回 <c>true</c></returns>
    public bool TryGetGuid(out Guid value)
    {
        if (Kind == DataType.Guid)
        {
            value = _guid;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     尝试获取 Bool 值
    /// </summary>
    /// <param name="value">输出值</param>
    /// <returns>类型匹配时返回 <c>true</c></returns>
    public bool TryGetBool(out bool value)
    {
        if (Kind == DataType.Bool)
        {
            value = _bool;
            return true;
        }

        value = default;
        return false;
    }

    #endregion

    #region 隐式转换

    /// <summary>
    ///     从 <see cref="long" /> 隐式创建 <see cref="DataValue" />
    /// </summary>
    public static implicit operator DataValue(long value)
    {
        return new DataValue(DataType.Int64, value);
    }

    /// <summary>
    ///     从 <see cref="double" /> 隐式创建 <see cref="DataValue" />
    /// </summary>
    public static implicit operator DataValue(double value)
    {
        return new DataValue(DataType.Float64, float64: value);
    }

    /// <summary>
    ///     从 <see cref="string" /> 隐式创建 <see cref="DataValue" />，<c>null</c> 字符串转换为 Null 类型
    /// </summary>
    public static implicit operator DataValue(string? value)
    {
        if (value is null) return new DataValue(DataType.Null);

        return new DataValue(DataType.String, str: value);
    }

    /// <summary>
    ///     从 <see cref="byte" /> 数组隐式创建 <see cref="DataValue" />，<c>null</c> 数组转换为 Null 类型
    /// </summary>
    public static implicit operator DataValue(byte[]? value)
    {
        if (value is null) return new DataValue(DataType.Null);

        return new DataValue(DataType.Bytes, bytes: value);
    }

    /// <summary>
    ///     从 <see cref="Guid" /> 隐式创建 <see cref="DataValue" />
    /// </summary>
    public static implicit operator DataValue(Guid value)
    {
        return new DataValue(DataType.Guid, guid: value);
    }

    /// <summary>
    ///     从 <see cref="bool" /> 隐式创建 <see cref="DataValue" />
    /// </summary>
    public static implicit operator DataValue(bool value)
    {
        return new DataValue(DataType.Bool, b: value);
    }

    #endregion

    #region 相等性

    /// <summary>
    ///     比较两个 <see cref="DataValue" /> 是否相等
    /// </summary>
    public bool Equals(DataValue other)
    {
        if (Kind != other.Kind) return false;

        return Kind switch
        {
            DataType.Int64 => _int64 == other._int64,
            DataType.Float64 => _float64.Equals(other._float64),
            DataType.String => string.Equals(_string, other._string, StringComparison.Ordinal),
            DataType.Bytes => BytesEqual(_bytes, other._bytes),
            DataType.Guid => _guid.Equals(other._guid),
            DataType.Bool => _bool == other._bool,
            DataType.Null => true,
            _ => false
        };
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is DataValue other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Kind switch
        {
            DataType.Int64 => HashCode.Combine(Kind, _int64),
            DataType.Float64 => HashCode.Combine(Kind, _float64),
            DataType.String => HashCode.Combine(Kind, _string),
            DataType.Bytes => HashCode.Combine(Kind, _bytes),
            DataType.Guid => HashCode.Combine(Kind, _guid),
            DataType.Bool => HashCode.Combine(Kind, _bool),
            DataType.Null => HashCode.Combine(Kind),
            _ => 0
        };
    }

    /// <summary>
    ///     比较两个 <see cref="DataValue" /> 是否相等
    /// </summary>
    public static bool operator ==(DataValue left, DataValue right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     比较两个 <see cref="DataValue" /> 是否不相等
    /// </summary>
    public static bool operator !=(DataValue left, DataValue right)
    {
        return !left.Equals(right);
    }

    #endregion

    #region ToString

    /// <summary>
    ///     返回当前值的字符串表示
    /// </summary>
    public override string ToString()
    {
        return Kind switch
        {
            DataType.Int64 => _int64.ToString(),
            DataType.Float64 => _float64.ToString("G"),
            DataType.String => _string ?? string.Empty,
            DataType.Bytes => _bytes is not null ? Convert.ToHexString(_bytes) : string.Empty,
            DataType.Guid => _guid.ToString(),
            DataType.Bool => _bool ? "true" : "false",
            DataType.Null => "null",
            _ => string.Empty
        };
    }

    #endregion

    #region 辅助方法

    private static bool BytesEqual(byte[]? left, byte[]? right)
    {
        if (ReferenceEquals(left, right)) return true;

        if (left is null || right is null) return false;

        if (left.Length != right.Length) return false;

        for (var i = 0; i < left.Length; i++)
            if (left[i] != right[i])
                return false;

        return true;
    }

    #endregion
}

#endregion