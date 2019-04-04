namespace Std.Data.Binary;

/// <summary>
///     二进制格式检测器接口。基于魔数（Magic Bytes）识别二进制数据是否属于特定格式。
/// </summary>
public interface IDetector
{
    /// <summary>
    ///     检测输入字节数据是否属于当前检测器对应的格式。
    /// </summary>
    /// <param name="header">头部字节数据。</param>
    /// <returns>如果数据属于当前检测器对应的格式则返回 <c>true</c>。</returns>
    bool detect(ReadOnlySpan<byte> header);
}