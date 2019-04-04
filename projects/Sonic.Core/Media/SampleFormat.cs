namespace Core.Media;

/// <summary>
///     采样格式枚举，定义音频采样的数据类型。
/// </summary>
public enum SampleFormat
{
    /// <summary>
    ///     32 位浮点采样。
    /// </summary>
    float32,

    /// <summary>
    ///     64 位浮点采样。
    /// </summary>
    float64,

    /// <summary>
    ///     16 位有符号整数采样。
    /// </summary>
    int16,

    /// <summary>
    ///     32 位有符号整数采样。
    /// </summary>
    int32,

    /// <summary>
    ///     8 位无符号整数采样。
    /// </summary>
    u_int8
}