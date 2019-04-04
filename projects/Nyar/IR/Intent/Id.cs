namespace Nyar.IR.Intent;

/// <summary>
///     E-Graph 中的等价类标识符
/// </summary>
public readonly struct Id : IEquatable<Id>, IComparable<Id>
{
    /// <summary>
    ///     标识符值
    /// </summary>
    public uint value { get; }

    /// <summary>
    ///     初始化 Id 实例
    /// </summary>
    /// <param name="value">标识符值。</param>
    public Id(uint value)
    {
        this.value = value;
    }

    /// <summary>
    ///     从 uint 隐式转换为 Id
    /// </summary>
    /// <param name="value">标识符值。</param>
    public static implicit operator Id(uint value)
    {
        return new Id(value);
    }

    /// <summary>
    ///     从 Id 显式转换为 uint
    /// </summary>
    /// <param name="id">标识符。</param>
    public static explicit operator uint(Id id)
    {
        return id.value;
    }

    /// <summary>
    ///     判断当前实例是否等于另一个 Id
    /// </summary>
    /// <param name="other">另一个 Id。</param>
    /// <returns>是否相等。</returns>
    public bool Equals(Id other)
    {
        return value == other.value;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Id other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return value.GetHashCode();
    }

    /// <summary>
    ///     比较当前实例与另一个 Id 的大小
    /// </summary>
    /// <param name="other">另一个 Id。</param>
    /// <returns>比较结果。</returns>
    public int CompareTo(Id other)
    {
        return value.CompareTo(other.value);
    }

    /// <summary>
    ///     判断两个 Id 是否相等
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>是否相等。</returns>
    public static bool operator ==(Id left, Id right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     判断两个 Id 是否不相等
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>是否不相等。</returns>
    public static bool operator !=(Id left, Id right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    ///     判断左操作数是否小于右操作数
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>是否小于。</returns>
    public static bool operator <(Id left, Id right)
    {
        return left.value < right.value;
    }

    /// <summary>
    ///     判断左操作数是否小于等于右操作数
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>是否小于等于。</returns>
    public static bool operator <=(Id left, Id right)
    {
        return left.value <= right.value;
    }

    /// <summary>
    ///     判断左操作数是否大于右操作数
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>是否大于。</returns>
    public static bool operator >(Id left, Id right)
    {
        return left.value > right.value;
    }

    /// <summary>
    ///     判断左操作数是否大于等于右操作数
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>是否大于等于。</returns>
    public static bool operator >=(Id left, Id right)
    {
        return left.value >= right.value;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return value.ToString();
    }
}