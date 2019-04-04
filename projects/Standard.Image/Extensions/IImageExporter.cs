using Core.Media.Image;

namespace Std.Image.Extensions;

/// <summary>
///     图像导出器接口，用于将图像导出到外部对象。
/// </summary>
public interface IImageExporter
{
    /// <summary>
    ///     判断是否可以导出到指定类型。
    /// </summary>
    /// <param name="target_type">目标类型。</param>
    /// <param name="image">源图像。</param>
    /// <returns>是否可以导出。</returns>
    bool can_export(Type target_type, IImage image);

    /// <summary>
    ///     将图像导出到外部对象。
    /// </summary>
    /// <param name="image">源图像。</param>
    /// <param name="target_type">目标类型。</param>
    /// <returns>导出的对象。</returns>
    object export(IImage image, Type target_type);
}