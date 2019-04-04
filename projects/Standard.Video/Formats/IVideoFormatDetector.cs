namespace Std.Video.Formats;

/// <summary>
///     视频格式探测器接口，提供基于文件头或扩展名的格式检测能力。
/// </summary>
public interface IVideoFormatDetector
{
    /// <summary>
    ///     获取格式名称。
    /// </summary>
    string format_name { get; }

    /// <summary>
    ///     获取支持的文件扩展名集合。
    /// </summary>
    IEnumerable<string> file_extensions { get; }

    /// <summary>
    ///     根据文件头数据检测视频格式。
    /// </summary>
    /// <param name="header">文件头数据。</param>
    /// <returns>是否匹配该格式。</returns>
    bool detect(ReadOnlySpan<byte> header);

    /// <summary>
    ///     根据文件扩展名检测视频格式。
    /// </summary>
    /// <param name="extension">文件扩展名。</param>
    /// <returns>是否匹配该格式。</returns>
    bool detect(string extension);
}