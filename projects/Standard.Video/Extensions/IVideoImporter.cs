using Core.Media.Video;

namespace Std.Video.Extensions;

/// <summary>
///     视频导入器接口，提供从外部对象导入视频帧的能力。
/// </summary>
public interface IVideoImporter
{
    /// <summary>
    ///     判断是否能从指定类型导入视频帧。
    /// </summary>
    /// <param name="sourceType">源对象类型。</param>
    /// <returns>是否能导入。</returns>
    bool can_import(Type sourceType);

    /// <summary>
    ///     从外部对象导入视频帧。
    /// </summary>
    /// <param name="source">源对象。</param>
    /// <returns>导入的视频帧。</returns>
    IVideoFrame import(object source);
}