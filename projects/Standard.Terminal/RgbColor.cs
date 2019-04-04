namespace Std.Terminal;

/// <summary>
///     RGB 颜色值，用于终端渲染
/// </summary>
public readonly struct RgbColor : IEquatable<RgbColor>
{
    /// <summary>
    ///     红色分量
    /// </summary>
    public byte r { get; }

    /// <summary>
    ///     绿色分量
    /// </summary>
    public byte g { get; }

    /// <summary>
    ///     蓝色分量
    /// </summary>
    public byte b { get; }

    /// <summary>
    ///     创建 RGB 颜色
    /// </summary>
    /// <param name="r">红色分量</param>
    /// <param name="g">绿色分量</param>
    /// <param name="b">蓝色分量</param>
    public RgbColor(int r, int g, int b)
    {
        this.r = (byte)r;
        this.g = (byte)g;
        this.b = (byte)b;
    }

    /// <summary>
    ///     黑色
    /// </summary>
    public static RgbColor black { get; } = new(0, 0, 0);

    /// <summary>
    ///     白色
    /// </summary>
    public static RgbColor white { get; } = new(255, 255, 255);

    /// <summary>
    ///     红色
    /// </summary>
    public static RgbColor red { get; } = new(220, 50, 50);

    /// <summary>
    ///     绿色
    /// </summary>
    public static RgbColor green { get; } = new(0, 170, 70);

    /// <summary>
    ///     蓝色
    /// </summary>
    public static RgbColor blue { get; } = new(0, 120, 220);

    /// <summary>
    ///     灰色
    /// </summary>
    public static RgbColor gray { get; } = new(150, 150, 150);

    /// <summary>
    ///     深灰色
    /// </summary>
    public static RgbColor dark_gray { get; } = new(100, 100, 100);

    /// <summary>
    ///     黄色
    /// </summary>
    public static RgbColor yellow { get; } = new(230, 180, 0);

    /// <summary>
    ///     青色
    /// </summary>
    public static RgbColor cyan { get; } = new(0, 200, 200);

    /// <summary>
    ///     品红色
    /// </summary>
    public static RgbColor magenta { get; } = new(200, 50, 200);

    /// <summary>
    ///     深红色
    /// </summary>
    public static RgbColor dark_red { get; } = new(139, 0, 0);

    /// <summary>
    ///     深绿色
    /// </summary>
    public static RgbColor dark_green { get; } = new(0, 100, 0);

    /// <summary>
    ///     深黄色
    /// </summary>
    public static RgbColor dark_yellow { get; } = new(180, 140, 0);

    /// <summary>
    ///     深蓝色
    /// </summary>
    public static RgbColor dark_blue { get; } = new(0, 0, 139);

    /// <summary>
    ///     深青色
    /// </summary>
    public static RgbColor dark_cyan { get; } = new(0, 139, 139);

    /// <summary>
    ///     默认颜色（白色）
    /// </summary>
    public static RgbColor Default => white;

    /// <summary>
    ///     红色分量（PascalCase 别名）
    /// </summary>
    public byte R => r;

    /// <summary>
    ///     绿色分量（PascalCase 别名）
    /// </summary>
    public byte G => g;

    /// <summary>
    ///     蓝色分量（PascalCase 别名）
    /// </summary>
    public byte B => b;

    /// <summary>
    ///     黑色（PascalCase 别名）
    /// </summary>
    public static RgbColor Black => black;

    /// <summary>
    ///     白色（PascalCase 别名）
    /// </summary>
    public static RgbColor White => white;

    /// <summary>
    ///     红色（PascalCase 别名）
    /// </summary>
    public static RgbColor Red => red;

    /// <summary>
    ///     绿色（PascalCase 别名）
    /// </summary>
    public static RgbColor Green => green;

    /// <summary>
    ///     蓝色（PascalCase 别名）
    /// </summary>
    public static RgbColor Blue => blue;

    /// <summary>
    ///     灰色（PascalCase 别名）
    /// </summary>
    public static RgbColor Gray => gray;

    /// <summary>
    ///     深灰色（PascalCase 别名）
    /// </summary>
    public static RgbColor DarkGray => dark_gray;

    /// <summary>
    ///     黄色（PascalCase 别名）
    /// </summary>
    public static RgbColor Yellow => yellow;

    /// <summary>
    ///     青色（PascalCase 别名）
    /// </summary>
    public static RgbColor Cyan => cyan;

    /// <summary>
    ///     品红色（PascalCase 别名）
    /// </summary>
    public static RgbColor Magenta => magenta;

    /// <summary>
    ///     深红色（PascalCase 别名）
    /// </summary>
    public static RgbColor DarkRed => dark_red;

    /// <summary>
    ///     深绿色（PascalCase 别名）
    /// </summary>
    public static RgbColor DarkGreen => dark_green;

    /// <summary>
    ///     深黄色（PascalCase 别名）
    /// </summary>
    public static RgbColor DarkYellow => dark_yellow;

    /// <summary>
    ///     深蓝色（PascalCase 别名）
    /// </summary>
    public static RgbColor DarkBlue => dark_blue;

    /// <summary>
    ///     深青色（PascalCase 别名）
    /// </summary>
    public static RgbColor DarkCyan => dark_cyan;

    /// <inheritdoc />
    public bool Equals(RgbColor other)
    {
        return r == other.r && g == other.g && b == other.b;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is RgbColor other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(r, g, b);
    }

    /// <summary>
    ///     比较两个 RgbColor 是否相等
    /// </summary>
    public static bool operator ==(RgbColor left, RgbColor right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     比较两个 RgbColor 是否不相等
    /// </summary>
    public static bool operator !=(RgbColor left, RgbColor right)
    {
        return !left.Equals(right);
    }
}