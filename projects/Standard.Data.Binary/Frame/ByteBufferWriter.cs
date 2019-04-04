using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using Std.Codec;

namespace Std.Data.Binary.Frame;

/// <summary>
///     内存写入缓冲区，基于 <see cref="Span{T}" /> 提供零分配的二进制数据写入能力的
/// </summary>
/// <remarks>
///     <para>
///         ByteBufferWriter 的ByteBuffer 的对称写入组件，所有写入方法使�?see cref="Span{T}" /> �?
///         <see cref="BinaryPrimitives" />，避免任何堆分配和流包装开销�?
///     </para>
///     <para>
///         当写入超出缓冲区容量时，自动扩容为原来的 2 倍，确保编码器无需预计算精确大小的
///     </para>
///     <para>
///         热路径方法标�?see cref="MethodImplOptions.AggressiveInlining" /> 以确的JIT 内联�?
///     </para>
/// </remarks>
public ref struct ByteBufferWriter
{
    private byte[] _buffer;

    /// <summary>
    ///     初始�?see cref="ByteBufferWriter" /> 结构的新实例，直接使用传入的缓冲区的
    /// </summary>
    /// <param name="buffer">
    ///     要写入的目标字节缓冲区（直接引用，不复制）的/param>
    ///     <param name="endianness">字节序，默认为小端序�?param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBufferWriter(byte[] buffer, Endianness endianness = Endianness.little_endian)
    {
        _buffer = buffer;
        position = 0;
        this.endianness = endianness;
    }

    /// <summary>
    ///     初始�?see cref="ByteBufferWriter" /> 结构的新实例，复制传入的缓冲区的
    /// </summary>
    /// <param name="buffer">
    ///     要写入的目标字节缓冲区（会被复制）的/param>
    ///     <param name="endianness">字节序，默认为小端序�?param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBufferWriter(Span<byte> buffer, Endianness endianness = Endianness.little_endian)
    {
        _buffer = [.. buffer];
        position = 0;
        this.endianness = endianness;
    }

    /// <summary>
    ///     初始�?see cref="ByteBufferWriter" /> 结构的新实例�?
    /// </summary>
    /// <param name="capacity">
    ///     初始容量（字节）�?param>
    ///     <param name="endianness">字节序，默认为小端序�?param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBufferWriter(int capacity, Endianness endianness = Endianness.little_endian)
    {
        _buffer = new byte[capacity];
        position = 0;
        this.endianness = endianness;
    }

    /// <summary>
    ///     获取当前写入位置�?
    /// </summary>
    public int position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    /// <summary>
    ///     获取缓冲区总长度的
    /// </summary>
    public int length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.Length;
    }

    /// <summary>
    ///     获取剩余可写入的字节数的
    /// </summary>
    public int remaining
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.Length - position;
    }

    /// <summary>
    ///     获取或设置字节序。通用写入方法（如 <see cref="write_u16" />的see cref="WriteU32" /> 等）
    ///     根据此属性选择小端序或大端序。默认为小端序的
    /// </summary>
    public Endianness endianness
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set;
    }

    /// <summary>
    ///     获取已写入数据的只读视图�?
    /// </summary>
    public ReadOnlySpan<byte> written_data => new(_buffer, 0, position);

    #region 基础写入

    /// <summary>
    ///     获取从当前位置开始的写入 Span�?
    /// </summary>
    /// <param name="sizeHint">
    ///     期望的写入大小，0 或负数表示剩余全部空间的/param>
    ///     <returns>可写入的字节 Span�?returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> get_span(int sizeHint = 0)
    {
        var size = sizeHint <= 0 ? _buffer.Length - position : sizeHint;
        ensure_capacity(size);
        return _buffer.AsSpan(position, size);
    }

    /// <summary>
    ///     向前移动指定字节数的写入位置�?
    /// </summary>
    /// <param name="bytes">要前进的字节数的/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void advance(int bytes)
    {
        if (bytes < 0) throw new ArgumentOutOfRangeException(nameof(bytes), "前进字节数不能为负数");

        position += bytes;
    }

    /// <summary>
    ///     将字节数据写入缓冲区并前进相应字节数�?
    /// </summary>
    /// <param name="data">要写入的字节数据�?param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write(ReadOnlySpan<byte> data)
    {
        ensure_capacity(data.Length);
        data.CopyTo(_buffer.AsSpan(position));
        position += data.Length;
    }

    #endregion

    #region 无符号整数写�?

    /// <summary>
    ///     写入一个无符号 8 位整数并前进 1 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u8(byte value)
    {
        ensure_capacity(1);
        _buffer[position++] = value;
    }

    /// <summary>
    ///     以小端序写入一个无符号 16 位整数并前进 2 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u16_le(ushort value)
    {
        ensure_capacity(2);
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(position), value);
        position += 2;
    }

    /// <summary>
    ///     以大端序写入一个无符号 16 位整数并前进 2 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u16_be(ushort value)
    {
        ensure_capacity(2);
        BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(position), value);
        position += 2;
    }

    /// <summary>
    ///     以小端序写入一个无符号 32 位整数并前进 4 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u32_le(uint value)
    {
        ensure_capacity(4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(position), value);
        position += 4;
    }

    /// <summary>
    ///     以大端序写入一个无符号 32 位整数并前进 4 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u32_be(uint value)
    {
        ensure_capacity(4);
        BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(position), value);
        position += 4;
    }

    /// <summary>
    ///     以小端序写入一个无符号 64 位整数并前进 8 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u64_le(ulong value)
    {
        ensure_capacity(8);
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(position), value);
        position += 8;
    }

    /// <summary>
    ///     以大端序写入一个无符号 64 位整数并前进 8 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u64_be(ulong value)
    {
        ensure_capacity(8);
        BinaryPrimitives.WriteUInt64BigEndian(_buffer.AsSpan(position), value);
        position += 8;
    }

    #endregion

    #region 有符号整数写�?

    /// <summary>
    ///     写入一个有符号 8 位整数并前进 1 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_i8(sbyte value)
    {
        write_u8((byte)value);
    }

    /// <summary>
    ///     以小端序写入一个有符号 16 位整数并前进 2 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_i16_le(short value)
    {
        ensure_capacity(2);
        BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan(position), value);
        position += 2;
    }

    /// <summary>
    ///     以大端序写入一个有符号 16 位整数并前进 2 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_i16_be(short value)
    {
        ensure_capacity(2);
        BinaryPrimitives.WriteInt16BigEndian(_buffer.AsSpan(position), value);
        position += 2;
    }

    /// <summary>
    ///     以小端序写入一个有符号 32 位整数并前进 4 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_i32_le(int value)
    {
        ensure_capacity(4);
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(position), value);
        position += 4;
    }

    /// <summary>
    ///     以大端序写入一个有符号 32 位整数并前进 4 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_i32_be(int value)
    {
        ensure_capacity(4);
        BinaryPrimitives.WriteInt32BigEndian(_buffer.AsSpan(position), value);
        position += 4;
    }

    /// <summary>
    ///     以小端序写入一个有符号 64 位整数并前进 8 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_i64_le(long value)
    {
        ensure_capacity(8);
        BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(position), value);
        position += 8;
    }

    /// <summary>
    ///     以大端序写入一个有符号 64 位整数并前进 8 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_i64_be(long value)
    {
        ensure_capacity(8);
        BinaryPrimitives.WriteInt64BigEndian(_buffer.AsSpan(position), value);
        position += 8;
    }

    #endregion

    #region 浮点数写�?

    /// <summary>
    ///     以小端序写入一�?6 位半精度浮点数并前进 2 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_f16_le(Half value)
    {
        ensure_capacity(2);
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(position), BitConverter.HalfToUInt16Bits(value));
        position += 2;
    }

    /// <summary>
    ///     以大端序写入一�?6 位半精度浮点数并前进 2 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_f16_be(Half value)
    {
        ensure_capacity(2);
        BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(position), BitConverter.HalfToUInt16Bits(value));
        position += 2;
    }

    /// <summary>
    ///     以小端序写入一�?2 位浮点数并前�? 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_f32_le(float value)
    {
        ensure_capacity(4);
        BinaryPrimitives.WriteSingleLittleEndian(_buffer.AsSpan(position), value);
        position += 4;
    }

    /// <summary>
    ///     以大端序写入一�?2 位浮点数并前�? 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_f32_be(float value)
    {
        ensure_capacity(4);
        BinaryPrimitives.WriteSingleBigEndian(_buffer.AsSpan(position), value);
        position += 4;
    }

    /// <summary>
    ///     以小端序写入一�?4 位浮点数并前�? 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_f64_le(double value)
    {
        ensure_capacity(8);
        BinaryPrimitives.WriteDoubleLittleEndian(_buffer.AsSpan(position), value);
        position += 8;
    }

    /// <summary>
    ///     以大端序写入一�?4 位浮点数并前�? 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_f64_be(double value)
    {
        ensure_capacity(8);
        BinaryPrimitives.WriteDoubleBigEndian(_buffer.AsSpan(position), value);
        position += 8;
    }

    #endregion

    #region 通用字节序写�?

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序写入一个无符号 16 位整数并前进 2 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u16(ushort value)
    {
        if (endianness == Endianness.little_endian)
            write_u16_le(value);
        else
            write_u16_be(value);
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序写入一个无符号 32 位整数并前进 4 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u32(uint value)
    {
        if (endianness == Endianness.little_endian)
            write_u32_le(value);
        else
            write_u32_be(value);
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序写入一个无符号 64 位整数并前进 8 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u64(ulong value)
    {
        if (endianness == Endianness.little_endian)
            write_u64_le(value);
        else
            write_u64_be(value);
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序写入一个有符号 16 位整数并前进 2 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_i16(short value)
    {
        if (endianness == Endianness.little_endian)
            write_i16_le(value);
        else
            write_i16_be(value);
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序写入一个有符号 32 位整数并前进 4 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_i32(int value)
    {
        if (endianness == Endianness.little_endian)
            write_i32_le(value);
        else
            write_i32_be(value);
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序写入一个有符号 64 位整数并前进 8 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_i64(long value)
    {
        if (endianness == Endianness.little_endian)
            write_i64_le(value);
        else
            write_i64_be(value);
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序写入一�?6 位半精度浮点数并前进 2 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_f16(Half value)
    {
        if (endianness == Endianness.little_endian)
            write_f16_le(value);
        else
            write_f16_be(value);
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序写入一�?2 位浮点数并前�? 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_f32(float value)
    {
        if (endianness == Endianness.little_endian)
            write_f32_le(value);
        else
            write_f32_be(value);
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序写入一�?4 位浮点数并前�? 字节�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_f64(double value)
    {
        if (endianness == Endianness.little_endian)
            write_f64_le(value);
        else
            write_f64_be(value);
    }

    #endregion

    #region LEB128 写入

    /// <summary>
    ///     直接写入一个字节到缓冲区指定位置，返回更新后的位置索引。不做容量检查，由调用方保证足够空间�?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int write_u8_internal(byte[] buffer, int position, byte value)
    {
        buffer[position] = value;
        return position + 1;
    }

    /// <summary>
    ///     以 LEB128 编码写入一个无符号 32 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_leb128_u32(uint value)
    {
        if (value < 0x80)
        {
            ensure_capacity(1);
            position = write_u8_internal(_buffer, position, (byte)value);
            return;
        }

        ensure_capacity(5);

        while (true)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0) b |= 0x80;

            _buffer[position++] = b;
            if (value == 0) break;
        }
    }

    /// <summary>
    ///     以 LEB128 编码写入一个无符号 64 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_leb128_u64(ulong value)
    {
        if (value < 0x80)
        {
            ensure_capacity(1);
            position = write_u8_internal(_buffer, position, (byte)value);
            return;
        }

        ensure_capacity(10);

        while (true)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0) b |= 0x80;

            _buffer[position++] = b;
            if (value == 0) break;
        }
    }

    /// <summary>
    ///     以 LEB128 编码写入一个有符号 32 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_leb128_i32(int value)
    {
        if (value is >= 0 and < 0x40)
        {
            ensure_capacity(1);
            position = write_u8_internal(_buffer, position, (byte)value);
            return;
        }

        ensure_capacity(5);
        var more = true;

        while (more)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;

            if ((value == 0 && (b & 0x40) == 0) || (value == -1 && (b & 0x40) != 0))
                more = false;
            else
                b |= 0x80;

            _buffer[position++] = b;
        }
    }

    /// <summary>
    ///     以 LEB128 编码写入一个有符号 64 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_leb128_i64(long value)
    {
        if (value is >= 0 and < 0x40)
        {
            ensure_capacity(1);
            position = write_u8_internal(_buffer, position, (byte)value);
            return;
        }

        ensure_capacity(10);
        var more = true;

        while (more)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;

            if ((value == 0 && (b & 0x40) == 0) || (value == -1 && (b & 0x40) != 0))
                more = false;
            else
                b |= 0x80;

            _buffer[position++] = b;
        }
    }

    /// <summary>
    ///     的ZigZag + LEB128 编码写入一个有符号 32 位整数并前进相应字节数的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_zig_zag_leb128_i32(int value)
    {
        var zigzag = (uint)((value << 1) ^ (value >> 31));
        write_leb128_u32(zigzag);
    }

    /// <summary>
    ///     的ZigZag + LEB128 编码写入一个有符号 64 位整数并前进相应字节数的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_zig_zag_leb128_i64(long value)
    {
        var zigzag = (ulong)((value << 1) ^ (value >> 63));
        write_leb128_u64(zigzag);
    }

    #endregion

    #region 字符串写�?

    /// <summary>
    ///     写入原始字节形式的字符串数据�?
    /// </summary>
    /// <param name="data">UTF-8 编码的字节数据的/param>
    public void write_string(ReadOnlySpan<byte> data)
    {
        write(data);
    }

    /// <summary>
    ///     将字符串的UTF-8 编码写入缓冲区（零分配）�?
    /// </summary>
    /// <param name="value">要写入的字符串的/param>
    public void write_string(string value)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        ensure_capacity(maxByteCount);
        var written = Encoding.UTF8.GetBytes(value, _buffer.AsSpan(position));
        position += written;
    }

    /// <summary>
    ///     写入的null 终止的UTF-8 字符串（零分配）�?
    /// </summary>
    /// <param name="value">要写入的字符串的/param>
    public void write_null_terminated_string(string value)
    {
        write_string(value);
        write_u8(0);
    }

    /// <summary>
    ///     写入 LEB128 长度前缀的UTF-8 字符串（零分配）�?
    /// </summary>
    /// <param name="value">要写入的字符串的/param>
    public void write_leb128_string(string value)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        ensure_capacity(maxByteCount + 5);

        var leb128Start = position;
        write_leb128_u32(0);
        var written = Encoding.UTF8.GetBytes(value, _buffer.AsSpan(position));
        position += written;

        var savedPosition = position;
        position = leb128Start;
        write_leb128_u32((uint)written);
        position = savedPosition;
    }

    #endregion

    #region 输出方法

    /// <summary>
    ///     将已写入的数据复制到新数组的
    /// </summary>
    /// <returns>包含已写入数据的字节数组�?returns>
    public byte[] to_array()
    {
        return [.. written_data];
    }

    #endregion

    #region 私有方法

    /// <summary>
    ///     确保缓冲区有足够的剩余容量，不足时自动扩容的
    /// </summary>
    /// <param name="needed">需要的额外字节数的/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ensure_capacity(int needed)
    {
        if (position + needed <= _buffer.Length) return;

        var newCapacity = System.Math.Max(_buffer.Length * 2, position + needed);
        var newBuffer = new byte[newCapacity];
        Array.Copy(_buffer, newBuffer, position);
        _buffer = newBuffer;
    }

    #endregion
}