using System.Collections.Generic;

namespace Core.Geo;

/// <summary>
///     地理多边形接口
/// </summary>
public interface IPolygon
{
    /// <summary>
    ///     外环顶点列表
    /// </summary>
    IReadOnlyList<IPoint> exterior_ring { get; }
}