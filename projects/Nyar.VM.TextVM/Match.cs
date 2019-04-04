namespace Nyar.VM.TextVM;

/// <summary>
/// 匹配结果，使用字节偏移量表示，指向原始输入切片（零拷贝）。
/// </summary>
public readonly struct Match
{
    /// <summary>
    /// 匹配起始位置（字节偏移量，包含）。
    /// </summary>
    public Int32 Start { get; }

    /// <summary>
    /// 匹配结束位置（字节偏移量，不包含）。
    /// </summary>
    public Int32 End { get; }

    /// <summary>
    /// 匹配长度（字节数）。
    /// </summary>
    public Int32 Length => End - Start;

    /// <summary>
    /// 使用指定的起始和结束偏移量创建匹配结果。
    /// </summary>
    /// <param name="start">起始字节偏移量（包含）。</param>
    /// <param name="end">结束字节偏移量（不包含）。</param>
    public Match(Int32 start, Int32 end)
    {
        Start = start;
        End = end;
    }
}
