namespace Core.Geo;

/// <summary>
///     坐标参考系统接口
/// </summary>
public interface ICoordinateReferenceSystem
{
    /// <summary>
    ///     坐标参考系统的 WKT 表示
    /// </summary>
    string wkt { get; }
}