namespace Std.Video.Vision;

/// <summary>
///     光流场类，存储两帧之间每个像素的运动向量。
/// </summary>
public sealed class OpticalFlowField
{
    /// <summary>
    ///     初始化 <see cref="OpticalFlowField" /> 的新实例。
    /// </summary>
    /// <param name="width">光流场宽度。</param>
    /// <param name="height">光流场高度。</param>
    public OpticalFlowField(int width, int height)
    {
        this.width = width;
        this.height = height;
        flow_x = new float[width * height];
        flow_y = new float[width * height];
    }

    /// <summary>
    ///     获取光流场宽度。
    /// </summary>
    public int width { get; }

    /// <summary>
    ///     获取光流场高度。
    /// </summary>
    public int height { get; }

    /// <summary>
    ///     水平方向光流数据。
    /// </summary>
    public float[] flow_x { get; }

    /// <summary>
    ///     垂直方向光流数据。
    /// </summary>
    public float[] flow_y { get; }

    /// <summary>
    ///     计算指定位置的光流向量的幅值。
    /// </summary>
    /// <param name="x">水平坐标。</param>
    /// <param name="y">垂直坐标。</param>
    /// <returns>光流向量的幅值。</returns>
    public float magnitude_at(int x, int y)
    {
        var fx = flow_x[y * width + x];
        var fy = flow_y[y * width + x];

        return (float)System.Math.Sqrt(fx * fx + fy * fy);
    }

    /// <summary>
    ///     计算指定位置的光流向量的角度（弧度）。
    /// </summary>
    /// <param name="x">水平坐标。</param>
    /// <param name="y">垂直坐标。</param>
    /// <returns>光流向量的角度（弧度）。</returns>
    public float angle_at(int x, int y)
    {
        var fx = flow_x[y * width + x];
        var fy = flow_y[y * width + x];

        return (float)System.Math.Atan2(fy, fx);
    }
}