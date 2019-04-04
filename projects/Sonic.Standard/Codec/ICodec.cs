namespace Std.Codec;

/// <summary>
///     泛型类型安全编解码器接口，基于 Span 提供零分配的编解码能力。
/// </summary>
/// <typeparam name="T">编解码的值类型。</typeparam>
public interface ICodec<T>
{
    /// <summary>
    ///     获取值的编码大小。固定大小返回正数，变长返回 -1。
    /// </summary>
    int get_size(T value);

    /// <summary>
    ///     将值编码到目标 Span。
    /// </summary>
    void encode(T value, Span<byte> destination);

    /// <summary>
    ///     从源 Span 解码值。
    /// </summary>
    T decode(ReadOnlySpan<byte> source);
}