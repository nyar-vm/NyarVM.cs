namespace Nyar.Analyzer.Utilities;

/// <summary>
///     缩进事件类型
/// </summary>
public enum IndentEvent
{
    /// <summary>
    ///     缩进增加
    /// </summary>
    indent,

    /// <summary>
    ///     缩进减少
    /// </summary>
    dedent
}