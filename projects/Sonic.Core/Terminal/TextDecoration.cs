using System;

namespace Core.Terminal;

/// <summary>
///     文本装饰枚举，支持按位组合的文本样式标志
/// </summary>
[Flags]
public enum TextDecoration
{
    /// <summary>
    ///     无装饰
    /// </summary>
    none = 0,

    /// <summary>
    ///     粗体
    /// </summary>
    bold = 1,

    /// <summary>
    ///     斜体
    /// </summary>
    italic = 2,

    /// <summary>
    ///     下划线
    /// </summary>
    underline = 4,

    /// <summary>
    ///     删除线
    /// </summary>
    strikethrough = 8
}