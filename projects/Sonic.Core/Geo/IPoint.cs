namespace Core.Geo;

/// <summary>
///     地理坐标点接口
/// </summary>
public interface IPoint
{
    /// <summary>
    ///     纬度
    /// </summary>
    double latitude { get; }

    /// <summary>
    ///     经度
    /// </summary>
    double longitude { get; }
}