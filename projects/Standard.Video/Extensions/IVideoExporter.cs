using Core.Media.Video;

namespace Std.Video.Extensions;

/// <summary>
///     视频导出器接口，提供将视频帧导出到外部对象的能力。
/// </summary>
public interface IVideoExporter
{
    /// <summary>
    ///     判断是否能将视频帧导出为指定类型。
    /// </summary>
    /// <param name="targetType">目标类型。</param>
    /// <param name="frame">待导出的视频帧。</param>
    /// <returns>是否能导出。</returns>
    bool can_export(Type targetType, IVideoFrame frame);

    /// <summary>
    ///     将视频帧导出为外部对象。
    /// </summary>
    /// <param name="frame">待导出的视频帧。</param>
    /// <param name="targetType">目标类型。</param>
    /// <returns>导出后的对象。</returns>
    object export(IVideoFrame frame, Type targetType);
}