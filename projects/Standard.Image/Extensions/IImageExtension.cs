namespace Std.Image.Extensions;

/// <summary>
///     图像扩展接口，作为第三方扩展的总装配入口。
/// </summary>
public interface IImageExtension
{
    /// <summary>
    ///     获取扩展名称。
    /// </summary>
    string name { get; }

    /// <summary>
    ///     注册扩展组件到上下文中。
    /// </summary>
    /// <param name="context">扩展注册上下文。</param>
    void register(ImageExtensionContext context);
}