using Core.Geo;

namespace Std.Geo;

/// <summary>
///     地理坐标点，实现 IPoint 接口，表示一个地理坐标
/// </summary>
public readonly struct Point : IPoint
{
    /// <summary>
    ///     纬度
    /// </summary>
    public double latitude { get; }

    /// <summary>
    ///     经度
    /// </summary>
    public double longitude { get; }

    /// <summary>
    ///     初始化地理坐标点
    /// </summary>
    /// <param name="latitude">纬度</param>
    /// <param name="longitude">经度</param>
    public Point(double latitude, double longitude)
    {
        this.latitude = latitude;
        this.longitude = longitude;
    }
}