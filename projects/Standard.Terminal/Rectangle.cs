namespace Std.Terminal;

/// <summary>
///     终端布局区域描述，定义渲染区域的坐标和尺寸�?///
/// </summary>
public readonly struct Rectangle : IEquatable<Rectangle>
{
    /// <summary>
    ///     获取区域左上角的列位置�?    ///
    /// </summary>
    public int x { get; }

    /// <summary>
    ///     获取区域左上角的行位置�?    ///
    /// </summary>
    public int y { get; }

    /// <summary>
    ///     获取区域的宽度�?    ///
    /// </summary>
    public int width { get; }

    /// <summary>
    ///     获取区域的高度�?    ///
    /// </summary>
    public int height { get; }

    /// <summary>
    ///     获取空的 <see cref="Rectangle" /> 实例�?    ///
    /// </summary>
    public static Rectangle empty { get; } = new(0, 0, 0, 0);

    /// <summary>
    ///     初始�?<see cref="Rectangle" /> 的新实例�?    ///
    /// </summary>
    /// <param name="x">
    ///     左上角列位置�?/param>
    ///     <param name="y">
    ///         左上角行位置�?/param>
    ///         <param name="width">
    ///             宽度�?/param>
    ///             <param name="height">高度�?/param>
    public Rectangle(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    /// <summary>
    ///     指示当前实例是否等于另一�?<see cref="Rectangle" />�?    ///
    /// </summary>
    public bool Equals(Rectangle other)
    {
        return x == other.x && y == other.y && width == other.width && height == other.height;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Rectangle other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, width, height);
    }
}