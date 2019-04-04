using Core.Geo;

namespace Std.Geo;

/// <summary>
///     地理多边形，实现 IPolygon 接口，表示一个地理区域
/// </summary>
public sealed class Polygon : IPolygon
{
    /// <summary>
    ///     初始化地理多边形
    /// </summary>
    /// <param name="exteriorRing">外环顶点列表</param>
    public Polygon(IReadOnlyList<IPoint> exteriorRing)
    {
        exterior_ring = exteriorRing;
    }

    /// <summary>
    ///     外环顶点列表
    /// </summary>
    public IReadOnlyList<IPoint> exterior_ring { get; }
}