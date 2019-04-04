using Std.DataProcess.Write;

namespace Std.DataProcess.Enframe;

/// <summary>
///     帧封装器：将载荷包装为帧�?/// 封帧器将载荷加上必要的帧边界（如长度前缀、分隔符）后写入 <see cref="IBufferWriter{Byte}" />�?///
/// </summary>
public interface IEnframer
{
    /// <summary>
    ///     将载荷加上帧边界后写入输出器�?    ///
    /// </summary>
    /// <param name="payload">
    ///     要封装的载荷数据�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void enframe(ReadOnlySpan<byte> payload, IBufferWriter<byte> writer);
}