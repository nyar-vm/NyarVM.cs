namespace Core.Text;

/// <summary>
///     文本视图接口，提供对文本片段的切片访问
/// </summary>
public interface ITextView
{
    /// <summary>
    ///     视图长度
    /// </summary>
    int length { get; }

    /// <summary>
    ///     从指定位置切取指定长度的文本视图
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="length">切片长度</param>
    /// <returns>新的文本视图</returns>
    IText slice(int start, int length);
}