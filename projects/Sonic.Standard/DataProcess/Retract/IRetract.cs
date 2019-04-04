namespace Std.DataProcess.Retract;

/// <summary>
///     单向还原器：从原始字节序列还原对�?///
/// </summary>
/// <typeparam name="T">要还原的类型</typeparam>
public interface IRetract<T>
{
    /// <summary>
    ///     从字节缓冲区还原指定类型的�?    ///
    /// </summary>
    /// <param name="buffer">
    ///     输入字节缓冲�?/param>
    ///     <returns>还原结果，包含值和消耗的字节数，BytesConsumed �?0 表示数据不足</returns>
    (T Value, int BytesConsumed) retract(ReadOnlySpan<byte> buffer);
}