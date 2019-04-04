namespace Core.Text;

/// <summary>
///     不可变文本的抽象接口，提供长度和编码信息
/// </summary>
public interface IText
{
    /// <summary>
    ///     文本长度
    /// </summary>
    int length { get; }

    /// <summary>
    ///     文本编码
    /// </summary>
    TextEncoding encoding { get; }
}