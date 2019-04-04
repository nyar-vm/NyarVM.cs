using Core.Media.Image;
using Std.Media;

namespace Std.Image.Formats;

/// <summary>
///     图像格式处理器接口，提供图像的加载和保存能力。
/// </summary>
public interface IImageFormatHandler
{
    /// <summary>
    ///     获取格式名称。
    /// </summary>
    string format_name { get; }

    /// <summary>
    ///     获取支持的文件扩展名列表。
    /// </summary>
    IEnumerable<string> file_extensions { get; }

    /// <summary>
    ///     从二进制数据加载图像。
    /// </summary>
    /// <param name="data">图像二进制数据。</param>
    /// <returns>加载的图像接口实例。</returns>
    IImage load(ReadOnlySpan<byte> data);

    /// <summary>
    ///     将图像保存为指定格式的二进制数据。
    /// </summary>
    /// <typeparam name="TPixel">像素类型。</typeparam>
    /// <param name="image">源图像。</param>
    /// <returns>编码后的二进制数据。</returns>
    byte[] save<TPixel>(Image<TPixel> image) where TPixel : unmanaged;
}