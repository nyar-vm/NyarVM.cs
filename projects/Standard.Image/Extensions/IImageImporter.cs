using Core.Media.Image;

namespace Std.Image.Extensions;

/// <summary>
///     图像导入器接口，用于从外部对象导入图像。
/// </summary>
public interface IImageImporter
{
    /// <summary>
    ///     判断是否可以从指定类型导入。
    /// </summary>
    /// <param name="source_type">源类型。</param>
    /// <returns>是否可以导入。</returns>
    bool can_import(Type source_type);

    /// <summary>
    ///     从外部对象导入图像。
    /// </summary>
    /// <param name="source">源对象。</param>
    /// <returns>导入的图像接口实例。</returns>
    IImage import(object source);
}