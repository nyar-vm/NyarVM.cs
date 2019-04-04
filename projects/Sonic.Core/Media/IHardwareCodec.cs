namespace Core.Media;

/// <summary>
///     硬件编解码器接口，提供硬件加速的编解码能力查询。
/// </summary>
public interface IHardwareCodec
{
    /// <summary>
    ///     获取编解码器名称。
    /// </summary>
    string codec_name { get; }

    /// <summary>
    ///     获取当前平台是否支持此硬件编解码器。
    /// </summary>
    bool is_supported { get; }
}