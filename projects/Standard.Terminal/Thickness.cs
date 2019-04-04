namespace Std.Terminal;

/// <summary>
///     四边厚度值，用于外边距等
/// </summary>
public readonly struct Thickness
{
    /// <summary>
    ///     上边值
    /// </summary>
    public int top { get; }

    /// <summary>
    ///     右边值
    /// </summary>
    public int right { get; }

    /// <summary>
    ///     下边值
    /// </summary>
    public int bottom { get; }

    /// <summary>
    ///     左边值
    /// </summary>
    public int left { get; }

    /// <summary>
    ///     零厚度
    /// </summary>
    public static readonly Thickness zero = new(0, 0, 0, 0);

    /// <summary>
    ///     创建四边厚度
    /// </summary>
    /// <param name="top">上边值</param>
    /// <param name="right">右边值</param>
    /// <param name="bottom">下边值</param>
    /// <param name="left">左边值</param>
    public Thickness(int top, int right, int bottom, int left)
    {
        this.top = top;
        this.right = right;
        this.bottom = bottom;
        this.left = left;
    }

    /// <summary>
    ///     四边统一值
    /// </summary>
    /// <param name="uniform">统一值</param>
    public Thickness(int uniform)
    {
        top = uniform;
        right = uniform;
        bottom = uniform;
        left = uniform;
    }
}