namespace Core.Media.Image;

/// <summary>
///     图像接口，提供图像的基本维度和格式信息。
/// </summary>
public interface IImage
{
    /// <summary>
    ///     获取图像宽度（像素）。
    /// </summary>
    int width { get; }

    /// <summary>
    ///     获取图像高度（像素）。
    /// </summary>
    int height { get; }

    /// <summary>
    ///     获取像素格式。
    /// </summary>
    PixelFormat format { get; }
}