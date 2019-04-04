namespace Std.DataProcess.Decode;

/// <summary>
///     解码结果，包装解码值和消耗的字节数。BytesConsumed �?0 表示数据不足
/// </summary>
/// <typeparam name="T">解码值的类型</typeparam>
public readonly struct Decoded<T>
{
    /// <summary>
    ///     解码得到的�?    ///
    /// </summary>
    public T value { get; }

    /// <summary>
    ///     实际消耗的字节数，0 表示缓冲区数据不�?    ///
    /// </summary>
    public int bytes_consumed { get; }

    /// <summary>
    ///     解码是否成功（有足够数据�?    ///
    /// </summary>
    public bool is_success => bytes_consumed > 0;

    /// <summary>
    ///     构造解码结�?    ///
    /// </summary>
    /// <param name="value">
    ///     解码�?/param>
    ///     <param name="bytesConsumed">消耗的字节�?/param>
    public Decoded(T value, int bytesConsumed)
    {
        this.value = value;
        bytes_consumed = bytesConsumed;
    }

    /// <summary>
    ///     数据不足时的失败结果
    /// </summary>
    public static Decoded<T> insufficient => new(default!, 0);
}

/// <summary>
///     原始字节解码结果，包装字节切片和消耗的字节数�?see cref="ReadOnlySpan{T}"/> �?ref struct 无法用作泛型参数，故特化为此类型�?///
/// </summary>
public readonly ref struct DecodedRawBytes
{
    /// <summary>
    ///     解码得到的字节切�?    ///
    /// </summary>
    public ReadOnlySpan<byte> value { get; }

    /// <summary>
    ///     实际消耗的字节数，0 表示缓冲区数据不�?    ///
    /// </summary>
    public int bytes_consumed { get; }

    /// <summary>
    ///     解码是否成功（有足够数据�?    ///
    /// </summary>
    public bool is_success => bytes_consumed > 0;

    /// <summary>
    ///     构造解码结�?    ///
    /// </summary>
    /// <param name="value">
    ///     解码得到的字节切�?/param>
    ///     <param name="bytesConsumed">消耗的字节�?/param>
    public DecodedRawBytes(ReadOnlySpan<byte> value, int bytesConsumed)
    {
        this.value = value;
        bytes_consumed = bytesConsumed;
    }

    /// <summary>
    ///     数据不足时的失败结果
    /// </summary>
    public static DecodedRawBytes insufficient => new(default, 0);
}