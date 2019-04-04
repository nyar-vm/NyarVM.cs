namespace Core.Text;

/// <summary>
///     文本构建器接口，用于逐步构建不可变文本
/// </summary>
public interface ITextBuilder
{
    /// <summary>
    ///     追加文本
    /// </summary>
    /// <param name="text">要追加的文本</param>
    /// <returns>当前构建器实例</returns>
    ITextBuilder append(string text);

    /// <summary>
    ///     构建最终的不变文本
    /// </summary>
    /// <returns>构建完成的文本</returns>
    IText build();
}