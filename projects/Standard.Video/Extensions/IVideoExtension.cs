namespace Std.Video.Extensions;

/// <summary>
///     视频扩展接口，作为第三方扩展的总装配入口。
/// </summary>
public interface IVideoExtension
{
    /// <summary>
    ///     获取扩展名称。
    /// </summary>
    string name { get; }

    /// <summary>
    ///     将扩展组件注册到视频扩展上下文中。
    /// </summary>
    /// <param name="context">视频扩展上下文。</param>
    void register(VideoExtensionContext context);
}