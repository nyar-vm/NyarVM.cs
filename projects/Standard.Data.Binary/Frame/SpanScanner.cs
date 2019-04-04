using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Std.Codec;

namespace Std.Data.Binary.Frame;

/// <summary>
///     通用二进制扫描器，封�?see cref="ByteBuffer" /> 并提供所有格式扫描器共享的基础方法�?
/// </summary>
/// <remarks>
///     <para>
///         由于 C# �?c>ref struct</c> 不支持继承，各格式扫描器无法通过基类共享代码�?
///         <see cref="SpanScanner" /> 通过组合模式解决此问题：各格式扫描器内部持有
///         <see cref="SpanScanner" /> 实例，通过 <see cref="P:buffer" /> 属性访问底�?
///         <see cref="ByteBuffer" /> 进行格式特定的读取操作的
///     </para>
///     <para>
///         本类型提供的方法是所有格式扫描器的公共子集：位置管理、魔数匹配、字节查看与读取�?
///         格式特定的读取操作（�?c>ReadU32LE</c>的c>ReadLeb128U32</c> 等）
///         通过 <see cref="P:buffer" /> 属性直接访�?see cref="ByteBuffer" /> 完成�?
///     </para>
/// </remarks>
public ref struct SpanScanner
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始�?see cref="SpanScanner" /> 结构的新实例�?
    /// </summary>
    /// <param name="data">
    ///     要扫描的字节数据�?param>
    ///     <param name="endianness">字节序，默认为小端序�?param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanScanner(ReadOnlySpan<byte> data, Endianness endianness = Endianness.little_endian)
    {
        _buffer = new ByteBuffer(data, endianness);
    }

    /// <summary>
    ///     获取或设置当前扫描位置的
    /// </summary>
    public int position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _buffer.position = value;
    }

    /// <summary>
    ///     获取数据总长度的
    /// </summary>
    public int length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.length;
    }

    /// <summary>
    ///     获取一个值，该值指示是否已到达数据末尾�?
    /// </summary>
    public bool is_end
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.is_end;
    }

    /// <summary>
    ///     获取剩余未读取的字节数的
    /// </summary>
    public int remaining_bytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.remaining;
    }

    /// <summary>
    ///     获取从当前位置到末尾的只读字的Span�?
    /// </summary>
    public ReadOnlySpan<byte> remaining
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.remaining_span;
    }

    /// <summary>
    ///     获取底层只读字节 Span�?
    /// </summary>
    public ReadOnlySpan<byte> data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.data;
    }

    /// <summary>
    ///     获取底层 <see cref="ByteBuffer" /> 的引用，用于格式特定的读取操作的
    /// </summary>
    /// <remarks>
    ///     通过 <c>ref</c> 返回确保�?see cref="ByteBuffer" /> 的修改（如位置前进）
    ///     直接反映�?see cref="SpanScanner" /> 的内部状态上�?
    /// </remarks>
    public ref ByteBuffer buffer
    {
        [UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _buffer;
    }

    /// <summary>
    ///     向前移动指定字节数，不返回任何数据的
    /// </summary>
    /// <param name="count">要跳过的字节数的/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void advance(int count)
    {
        _buffer.advance(count);
    }

    /// <summary>
    ///     查看接下来的若干字节但不移动位置�?
    /// </summary>
    /// <param name="count">
    ///     要查看的字节数的/param>
    ///     <returns>指定长度的只读字的Span�?returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> peek(int count)
    {
        return _buffer.peek(count);
    }

    /// <summary>
    ///     读取指定数量的字节并前进位置�?
    /// </summary>
    /// <param name="count">
    ///     要读取的字节数的/param>
    ///     <returns>指定长度的只读字的Span�?returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> read(int count)
    {
        return _buffer.read_bytes(count);
    }

    /// <summary>
    ///     尝试匹配魔数（Magic Number）的
    /// </summary>
    /// <param name="magic">
    ///     期望的魔数字节序列的/param>
    ///     <returns>如果匹配成功则返的true，否则返的false�?returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool match_magic(ReadOnlySpan<byte> magic)
    {
        return _buffer.match_magic(magic);
    }

    /// <summary>
    ///     尝试匹配魔数并自动前进位置的
    /// </summary>
    /// <param name="magic">
    ///     期望的魔数字节序列的/param>
    ///     <returns>如果匹配成功则返的true 并前进位置，否则返回 false�?returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool consume_magic(ReadOnlySpan<byte> magic)
    {
        return _buffer.consume_magic(magic);
    }
}