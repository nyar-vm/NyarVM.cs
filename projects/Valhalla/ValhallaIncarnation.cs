namespace Valhalla;

/// <summary>
///     包化身计数器，每次 PURGE 后重注册递增
/// </summary>
public readonly struct ValhallaIncarnation
{
    /// <summary>
    ///     化身编号（从 1 开始）
    /// </summary>
    public int number { get; }

    /// <summary>
    ///     创建化身计数器
    /// </summary>
    /// <param name="number">编号，必须 >= 1</param>
    public ValhallaIncarnation(int number)
    {
        if (number < 1) throw new ArgumentException("化身编号必须 >= 1", nameof(number));

        this.number = number;
    }

    /// <summary>
    ///     递增到下一个化身
    /// </summary>
    public ValhallaIncarnation next()
    {
        return new ValhallaIncarnation(number + 1);
    }

    /// <summary>
    ///     判断两个化身是否匹配（用于 lock 文件校验）
    /// </summary>
    public bool matches(ValhallaIncarnation other)
    {
        return number == other.number;
    }

    public override string ToString()
    {
        return $"incarnation:{number}";
    }

    public static bool operator ==(ValhallaIncarnation left, ValhallaIncarnation right)
    {
        return left.number == right.number;
    }

    public static bool operator !=(ValhallaIncarnation left, ValhallaIncarnation right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        return obj is ValhallaIncarnation other && number == other.number;
    }

    public override int GetHashCode()
    {
        return number.GetHashCode();
    }
}