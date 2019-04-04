namespace Std.DataProcess.Detect;

/// <summary>
///     格式检测器：在收到未知字节流时猜测其格式，返回置信度�?/// 多个检测器可组成检测链，选出最高置信度的格式�?///
/// </summary>
public interface IDetector
{
    /// <summary>
    ///     检测器对应的格式名称�?    ///
    /// </summary>
    string format_name { get; }

    bool detect(ReadOnlySpan<byte> data);

    /// <summary>
    ///     根据数据内容计算格式置信度�?    ///
    /// </summary>
    /// <param name="data">
    ///     待检测的字节数据�?/param>
    ///     <returns>0.0 �?1.0 之间的置信度值，1.0 表示完全确定�?/returns>
    float detect_confidence(ReadOnlySpan<byte> data)
    {
        return detect([.. data]) ? 1.0f : 0.0f;
    }
}