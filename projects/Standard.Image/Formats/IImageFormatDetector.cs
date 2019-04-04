namespace Std.Image.Formats;

/// <summary>
///     图像格式探测器接口，提供基于文件头或扩展名的格式识别能力。
/// </summary>
public interface IImageFormatDetector
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
    ///     根据文件头字节检测图像格式。
    /// </summary>
    /// <param name="header">文件头字节。</param>
    /// <returns>是否匹配该格式。</returns>
    bool detect(ReadOnlySpan<byte> header);

    /// <summary>
    ///     根据文件扩展名检测图像格式。
    /// </summary>
    /// <param name="extension">文件扩展名（不含前导点号）。</param>
    /// <returns>是否匹配该格式。</returns>
    bool detect(string extension);
}