using System.Runtime.CompilerServices;

namespace Std.Data.Binary.Frame;

/// <summary>
///     协议帧数据结构，表示二进制协议中的一个完整消息帧的
/// </summary>
/// <remarks>
///     <para>
///         Frame 使用 <c>ref struct</c> 以持的<see cref="ReadOnlySpan{T}" />的
///         确保零拷贝访问帧载荷数据。Frame 不能存储在堆上的
///     </para>
///     <para>
///         <see cref="size" /> 包含帧的完整大小（帧的+ 载荷），
///         <see cref="payload" /> 仅包含帧的载荷部分（去除帧头），
///         <see cref="raw" /> 包含帧的完整原始字节（帧的+ 载荷）的
///     </para>
/// </remarks>
public readonly ref struct Frame
{
    /// <summary>
    ///     初始的<see cref="Frame" /> 结构的新实例的
    /// </summary>
    /// <param name="size">
    ///     帧的完整大小（帧的+ 载荷）的/param>
    ///     <param name="payload">
    ///         帧的载荷数据的/param>
    ///         <param name="raw">帧的完整原始字节（帧的+ 载荷）的/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Frame(int size, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> raw)
    {
        this.size = size;
        this.payload = payload;
        this.raw = raw;
    }

    /// <summary>
    ///     初始的<see cref="Frame" /> 结构的新实例（不提供原始字节）的
    /// </summary>
    /// <param name="size">
    ///     帧的完整大小（帧的+ 载荷）的/param>
    ///     <param name="payload">帧的载荷数据的/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Frame(int size, ReadOnlySpan<byte> payload)
    {
        this.size = size;
        this.payload = payload;
        raw = default;
    }

    /// <summary>
    ///     帧的完整大小（字节），包含帧头和载荷的
    /// </summary>
    public int size { get; }

    /// <summary>
    ///     帧的载荷数据（去除帧头后的数据）的
    /// </summary>
    public ReadOnlySpan<byte> payload { get; }

    /// <summary>
    ///     帧的完整原始字节（帧的+ 载荷）的
    /// </summary>
    public ReadOnlySpan<byte> raw { get; }
}