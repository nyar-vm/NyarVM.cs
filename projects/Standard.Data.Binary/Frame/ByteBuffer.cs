using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Std.Codec;

namespace Std.Data.Binary.Frame;

/// <summary>
///     零拷贝内存缓冲区，基�?<see cref="ReadOnlySpan{T}" /> 提供零分配的二进制数据读取能力�?///
/// </summary>
/// <remarks>
///     <para>
///         ByteBuffer �?Acorn 扫描层的基础设施，所有格式扫描器应基�?ByteBuffer 构建�?///     所有读取方法使�?<see cref="ReadOnlySpan{T}" /> �?
///         <see cref="BinaryPrimitives" />�?///     避免任何堆分配和流包装开销�?///
///     </para>
///     <para>
///         热路径方法标�?<see cref="MethodImplOptions.AggressiveInlining" /> 以确�?JIT 内联�?///
///     </para>
/// </remarks>
public ref struct ByteBuffer
{
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>
    ///     初始�?<see cref="ByteBuffer" /> 结构的新实例�?    ///
    /// </summary>
    /// <param name="data">
    ///     要读取的字节数据�?/param>
    ///     <param name="endianness">字节序，默认为小端序�?/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer(ReadOnlySpan<byte> data, Endianness endianness = Endianness.little_endian)
    {
        _data = data;
        position = 0;
        this.endianness = endianness;
    }

    /// <summary>
    ///     获取或设置当前读取位置�?    ///
    /// </summary>
    public int position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set;
    }

    /// <summary>
    ///     获取数据总长度�?    ///
    /// </summary>
    public int length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data.Length;
    }

    /// <summary>
    ///     获取剩余未读取的字节数�?    ///
    /// </summary>
    public int remaining
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data.Length - position;
    }

    /// <summary>
    ///     获取一个值，该值指示是否已到达数据末尾�?    ///
    /// </summary>
    public bool is_end
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => position >= _data.Length;
    }

    /// <summary>
    ///     获取或设置字节序。通用读取方法（如 <see cref="read_u16" />的see cref="ReadU32" /> 等）
    ///     根据此属性选择小端序或大端序。默认为小端序�?    ///
    /// </summary>
    public Endianness endianness
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set;
    }

    /// <summary>
    ///     获取底层只读字节 Span�?    ///
    /// </summary>
    public ReadOnlySpan<byte> data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data;
    }

    /// <summary>
    ///     获取从当前位置到末尾的只读字�?Span�?    ///
    /// </summary>
    public ReadOnlySpan<byte> remaining_span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data[position..];
    }

    #region 前进与查�?

    /// <summary>
    ///     向前移动指定字节数，不返回任何数据�?    ///
    /// </summary>
    /// <param name="count">要跳过的字节数�?/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void advance(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "前进字节数不能为负数");

        var newPosition = position + count;

        if (newPosition > _data.Length) throw new InvalidOperationException("读取位置超出数据范围");

        position = newPosition;
    }

    /// <summary>
    ///     查看接下来的若干字节但不移动位置�?    ///
    /// </summary>
    /// <param name="count">
    ///     要查看的字节数的/param>
    ///     <returns>指定长度的只读字�?Span�?/returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> peek(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "查看字节数不能为负数");

        if (position + count > _data.Length) throw new InvalidOperationException("查看范围超出数据边界");

        return _data.Slice(position, count);
    }

    /// <summary>
    ///     读取指定数量的字节并前进位置�?    ///
    /// </summary>
    /// <param name="count">
    ///     要读取的字节数的/param>
    ///     <returns>指定长度的只读字�?Span�?/returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> read_bytes(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "读取字节数不能为负数");

        if (position + count > _data.Length) throw new InvalidOperationException("读取范围超出数据边界");

        var result = _data.Slice(position, count);
        position += count;
        return result;
    }

    #endregion

    #region 魔数匹配

    /// <summary>
    ///     尝试匹配魔数（Magic Number）�?    ///
    /// </summary>
    /// <param name="magic">
    ///     期望的魔数字节序列�?/param>
    ///     <returns>如果匹配成功则返�?true，否则返�?false�?/returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool match_magic(ReadOnlySpan<byte> magic)
    {
        if (magic.IsEmpty) return true;

        if (position + magic.Length > _data.Length) return false;

        return _data.Slice(position, magic.Length).SequenceEqual(magic);
    }

    /// <summary>
    ///     尝试匹配魔数并自动前进位置�?    ///
    /// </summary>
    /// <param name="magic">
    ///     期望的魔数字节序列�?/param>
    ///     <returns>如果匹配成功则返�?true 并前进位置，否则返回 false�?/returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool consume_magic(ReadOnlySpan<byte> magic)
    {
        if (!match_magic(magic)) return false;

        position += magic.Length;
        return true;
    }

    #endregion

    #region 无符号整数（小端序）

    /// <summary>
    ///     读取一个无符号 8 位整数并前进 1 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte read_u8()
    {
        if (position >= _data.Length) throw new InvalidOperationException("Unexpected end of data.");

        return _data[position++];
    }

    /// <summary>
    ///     以小端序读取一个无符号 16 位整数并前进 2 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort read_u16_le()
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(_data[position..]);
        position += 2;
        return value;
    }

    /// <summary>
    ///     以小端序读取一个无符号 32 位整数并前进 4 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint read_u32_le()
    {
        var value = BinaryPrimitives.ReadUInt32LittleEndian(_data[position..]);
        position += 4;
        return value;
    }

    /// <summary>
    ///     以小端序读取一个无符号 64 位整数并前进 8 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong read_u64_le()
    {
        var value = BinaryPrimitives.ReadUInt64LittleEndian(_data[position..]);
        position += 8;
        return value;
    }

    #endregion

    #region 无符号整数（大端序）

    /// <summary>
    ///     以大端序读取一个无符号 16 位整数并前进 2 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort read_u16_be()
    {
        var value = BinaryPrimitives.ReadUInt16BigEndian(_data[position..]);
        position += 2;
        return value;
    }

    /// <summary>
    ///     以大端序读取一个无符号 32 位整数并前进 4 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint read_u32_be()
    {
        var value = BinaryPrimitives.ReadUInt32BigEndian(_data[position..]);
        position += 4;
        return value;
    }

    /// <summary>
    ///     以大端序读取一个无符号 64 位整数并前进 8 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong read_u64_be()
    {
        var value = BinaryPrimitives.ReadUInt64BigEndian(_data[position..]);
        position += 8;
        return value;
    }

    #endregion

    #region 有符号整数（小端序）

    /// <summary>
    ///     读取一个有符号 8 位整数并前进 1 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte read_i8()
    {
        return (sbyte)read_u8();
    }

    /// <summary>
    ///     以小端序读取一个有符号 16 位整数并前进 2 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short read_i16_le()
    {
        var value = BinaryPrimitives.ReadInt16LittleEndian(_data[position..]);
        position += 2;
        return value;
    }

    /// <summary>
    ///     以小端序读取一个有符号 32 位整数并前进 4 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int read_i32_le()
    {
        var value = BinaryPrimitives.ReadInt32LittleEndian(_data[position..]);
        position += 4;
        return value;
    }

    /// <summary>
    ///     以小端序读取一个有符号 64 位整数并前进 8 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long read_i64_le()
    {
        var value = BinaryPrimitives.ReadInt64LittleEndian(_data[position..]);
        position += 8;
        return value;
    }

    #endregion

    #region 有符号整数（大端序）

    /// <summary>
    ///     以大端序读取一个有符号 16 位整数并前进 2 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short read_i16_be()
    {
        var value = BinaryPrimitives.ReadInt16BigEndian(_data[position..]);
        position += 2;
        return value;
    }

    /// <summary>
    ///     以大端序读取一个有符号 32 位整数并前进 4 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int read_i32_be()
    {
        var value = BinaryPrimitives.ReadInt32BigEndian(_data[position..]);
        position += 4;
        return value;
    }

    /// <summary>
    ///     以大端序读取一个有符号 64 位整数并前进 8 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long read_i64_be()
    {
        var value = BinaryPrimitives.ReadInt64BigEndian(_data[position..]);
        position += 8;
        return value;
    }

    #endregion

    #region 浮点�?

    /// <summary>
    ///     以小端序读取一�?6 位半精度浮点数并前进 2 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Half read_f16_le()
    {
        var bits = BinaryPrimitives.ReadUInt16LittleEndian(_data[position..]);
        position += 2;
        return BitConverter.UInt16BitsToHalf(bits);
    }

    /// <summary>
    ///     以大端序读取一�?6 位半精度浮点数并前进 2 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Half read_f16_be()
    {
        var bits = BinaryPrimitives.ReadUInt16BigEndian(_data[position..]);
        position += 2;
        return BitConverter.UInt16BitsToHalf(bits);
    }

    /// <summary>
    ///     以小端序读取一�?2 位浮点数并前�? 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float read_f32_le()
    {
        var value = BinaryPrimitives.ReadSingleLittleEndian(_data[position..]);
        position += 4;
        return value;
    }

    /// <summary>
    ///     以大端序读取一�?2 位浮点数并前�? 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float read_f32_be()
    {
        var value = BinaryPrimitives.ReadSingleBigEndian(_data[position..]);
        position += 4;
        return value;
    }

    /// <summary>
    ///     以小端序读取一�?4 位浮点数并前�? 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double read_f64_le()
    {
        var value = BinaryPrimitives.ReadDoubleLittleEndian(_data[position..]);
        position += 8;
        return value;
    }

    /// <summary>
    ///     以大端序读取一�?4 位浮点数并前�? 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double read_f64_be()
    {
        var value = BinaryPrimitives.ReadDoubleBigEndian(_data[position..]);
        position += 8;
        return value;
    }

    #endregion

    #region 通用字节序读�?

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序读取一个无符号 16 位整数并前进 2 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort read_u16()
    {
        return endianness == Endianness.little_endian ? read_u16_le() : read_u16_be();
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序读取一个无符号 32 位整数并前进 4 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint read_u32()
    {
        return endianness == Endianness.little_endian ? read_u32_le() : read_u32_be();
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序读取一个无符号 64 位整数并前进 8 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong read_u64()
    {
        return endianness == Endianness.little_endian ? read_u64_le() : read_u64_be();
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序读取一个有符号 16 位整数并前进 2 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short read_i16()
    {
        return endianness == Endianness.little_endian ? read_i16_le() : read_i16_be();
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序读取一个有符号 32 位整数并前进 4 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int read_i32()
    {
        return endianness == Endianness.little_endian ? read_i32_le() : read_i32_be();
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序读取一个有符号 64 位整数并前进 8 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long read_i64()
    {
        return endianness == Endianness.little_endian ? read_i64_le() : read_i64_be();
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序读取一�?6 位半精度浮点数并前进 2 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Half read_f16()
    {
        return endianness == Endianness.little_endian ? read_f16_le() : read_f16_be();
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序读取一�?2 位浮点数并前�? 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float read_f32()
    {
        return endianness == Endianness.little_endian ? read_f32_le() : read_f32_be();
    }

    /// <summary>
    ///     �?see cref="Endianness" /> 属性指定的字节序读取一�?4 位浮点数并前�? 字节�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double read_f64()
    {
        return endianness == Endianness.little_endian ? read_f64_le() : read_f64_be();
    }

    #endregion

    #region LEB128 变长整数

    /// <summary>
    ///     读取 LEB128 编码的无符号 32 位整数并前进相应字节数的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint read_leb128_u32()
    {
        if (position >= _data.Length)
            throw new InvalidOperationException("Unexpected end of data while reading LEB128.");

        var b = _data[position];

        if ((b & 0x80) == 0)
        {
            position++;
            return b;
        }

        uint result = 0;
        var shift = 0;

        while (true)
        {
            if (position >= _data.Length)
                throw new InvalidOperationException("Unexpected end of data while reading LEB128.");

            b = _data[position++];
            result |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0) break;

            shift += 7;

            if (shift >= 32) throw new InvalidDataException("LEB128 integer is too large.");
        }

        return result;
    }

    /// <summary>
    ///     读取 LEB128 编码的无符号 64 位整数并前进相应字节数的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong read_leb128_u64()
    {
        if (position >= _data.Length)
            throw new InvalidOperationException("Unexpected end of data while reading LEB128.");

        var b = _data[position];

        if ((b & 0x80) == 0)
        {
            position++;
            return b;
        }

        ulong result = 0;
        var shift = 0;

        while (true)
        {
            if (position >= _data.Length)
                throw new InvalidOperationException("Unexpected end of data while reading LEB128.");

            b = _data[position++];
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0) break;

            shift += 7;

            if (shift >= 64) throw new InvalidDataException("LEB128 integer is too large.");
        }

        return result;
    }

    /// <summary>
    ///     读取 LEB128 编码的有符号 32 位整数并前进相应字节数的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int read_leb128_i32()
    {
        if (position >= _data.Length)
            throw new InvalidOperationException("Unexpected end of data while reading LEB128.");

        var b = _data[position];

        if ((b & 0x80) == 0)
        {
            position++;

            var value = b & 0x7F;

            if ((b & 0x40) != 0) value |= ~0x7F;

            return value;
        }

        var result = 0;
        var shift = 0;

        do
        {
            if (position >= _data.Length)
                throw new InvalidOperationException("Unexpected end of data while reading LEB128.");

            b = _data[position++];
            result |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        if (shift < 32 && (b & 0x40) != 0) result |= ~0 << shift;

        return result;
    }

    /// <summary>
    ///     读取 ZigZag + LEB128 编码的有符号 32 位整数并前进相应字节数的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int read_zig_zag_leb128_i32()
    {
        var raw = read_leb128_u32();
        return (int)((raw >> 1) ^ (0u - (raw & 1)));
    }

    /// <summary>
    ///     读取 LEB128 编码的有符号 64 位整数并前进相应字节数的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long read_leb128_i64()
    {
        if (position >= _data.Length)
            throw new InvalidOperationException("Unexpected end of data while reading LEB128.");

        var b = _data[position];

        if ((b & 0x80) == 0)
        {
            position++;

            var value = (long)(b & 0x7F);

            if ((b & 0x40) != 0) value |= ~0x7FL;

            return value;
        }

        long result = 0;
        var shift = 0;

        do
        {
            if (position >= _data.Length)
                throw new InvalidOperationException("Unexpected end of data while reading LEB128.");

            b = _data[position++];
            result |= (long)(b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        if (shift < 64 && (b & 0x40) != 0) result |= ~0L << shift;

        return result;
    }

    /// <summary>
    ///     读取 ZigZag + LEB128 编码的有符号 64 位整数并前进相应字节数的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long read_zig_zag_leb128_i64()
    {
        var raw = read_leb128_u64();
        return (long)((raw >> 1) ^ (0UL - (raw & 1)));
    }

    /// <summary>
    ///     尝试从指定位置读的LEB128 编码的有符号 32 位整数，不移动位置的
    /// </summary>
    /// <param name="start">
    ///     起始位置�?param>
    ///     <param name="value">
    ///         解码后的值的/param>
    ///         <param name="consumed">
    ///             消耗的字节数的/param>
    ///             <returns>如果成功解码则返的true�?returns>
    public static bool try_decode_leb128_i32(ReadOnlySpan<byte> data, out int value, out int consumed)
    {
        value = 0;
        consumed = 0;
        var result = 0;
        var shift = 0;
        byte b;

        while (consumed < data.Length)
        {
            b = data[consumed];
            consumed++;
            result |= (b & 0x7F) << shift;
            shift += 7;

            if ((b & 0x80) == 0)
            {
                if (shift < 32 && (b & 0x40) != 0) result |= ~0 << shift;

                value = result;
                return true;
            }

            if (shift >= 32) return false;
        }

        return false;
    }

    /// <summary>
    ///     尝试从指定位置读的LEB128 编码的无符号 32 位整数，不移动位置的
    /// </summary>
    /// <param name="data">
    ///     数据源的/param>
    ///     <param name="value">
    ///         解码后的值的/param>
    ///         <param name="consumed">
    ///             消耗的字节数的/param>
    ///             <returns>如果成功解码则返的true�?returns>
    public static bool try_decode_leb128_u32(ReadOnlySpan<byte> data, out uint value, out int consumed)
    {
        value = 0;
        consumed = 0;
        uint result = 0;
        var shift = 0;

        while (consumed < data.Length)
        {
            var b = data[consumed];
            consumed++;
            result |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                value = result;
                return true;
            }

            shift += 7;

            if (shift >= 32) return false;
        }

        return false;
    }

    /// <summary>
    ///     尝试从指定位置读的LEB128 编码的无符号 64 位整数，不移动位置的
    /// </summary>
    /// <param name="data">
    ///     数据源的/param>
    ///     <param name="value">
    ///         解码后的值的/param>
    ///         <param name="consumed">
    ///             消耗的字节数的/param>
    ///             <returns>如果成功解码则返的true�?returns>
    public static bool try_decode_leb128_u64(ReadOnlySpan<byte> data, out ulong value, out int consumed)
    {
        value = 0;
        consumed = 0;
        ulong result = 0;
        var shift = 0;

        while (consumed < data.Length)
        {
            var b = data[consumed];
            consumed++;
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                value = result;
                return true;
            }

            shift += 7;

            if (shift >= 64) return false;
        }

        return false;
    }

    /// <summary>
    ///     尝试从指定位置读的LEB128 编码的有符号 64 位整数，不移动位置的
    /// </summary>
    /// <param name="data">
    ///     数据源的/param>
    ///     <param name="value">
    ///         解码后的值的/param>
    ///         <param name="consumed">
    ///             消耗的字节数的/param>
    ///             <returns>如果成功解码则返的true�?returns>
    public static bool try_decode_leb128_i64(ReadOnlySpan<byte> data, out long value, out int consumed)
    {
        value = 0;
        consumed = 0;
        long result = 0;
        var shift = 0;
        byte b;

        while (consumed < data.Length)
        {
            b = data[consumed];
            consumed++;
            result |= (long)(b & 0x7F) << shift;
            shift += 7;

            if ((b & 0x80) == 0)
            {
                if (shift < 64 && (b & 0x40) != 0) result |= ~0L << shift;

                value = result;
                return true;
            }

            if (shift >= 64) return false;
        }

        return false;
    }

    /// <summary>
    ///     尝试从指定位置读的ZigZag + LEB128 编码的有符号 32 位整数，不移动位置的
    /// </summary>
    /// <param name="data">
    ///     数据源的/param>
    ///     <param name="value">
    ///         解码后的值的/param>
    ///         <param name="consumed">
    ///             消耗的字节数的/param>
    ///             <returns>如果成功解码则返的true�?returns>
    public static bool try_decode_zig_zag_leb128_i32(ReadOnlySpan<byte> data, out int value, out int consumed)
    {
        if (!try_decode_leb128_u32(data, out var raw, out consumed))
        {
            value = 0;
            return false;
        }

        value = (int)((raw >> 1) ^ (0u - (raw & 1)));
        return true;
    }

    /// <summary>
    ///     尝试从指定位置读的ZigZag + LEB128 编码的有符号 64 位整数，不移动位置的
    /// </summary>
    /// <param name="data">
    ///     数据源的/param>
    ///     <param name="value">
    ///         解码后的值的/param>
    ///         <param name="consumed">
    ///             消耗的字节数的/param>
    ///             <returns>如果成功解码则返的true�?returns>
    public static bool try_decode_zig_zag_leb128_i64(ReadOnlySpan<byte> data, out long value, out int consumed)
    {
        if (!try_decode_leb128_u64(data, out var raw, out consumed))
        {
            value = 0;
            return false;
        }

        value = (long)((raw >> 1) ^ (0UL - (raw & 1)));
        return true;
    }

    #endregion

    #region 字符�?

    /// <summary>
    ///     读取指定字节长度的UTF-8 字符串并前进相应字节数的
    /// </summary>
    /// <param name="byteLength">
    ///     字符串的字节长度�?param>
    ///     <returns>解码后的字符串的/returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string read_string(int byteLength)
    {
        if (byteLength == 0) return string.Empty;

        var span = read_bytes(byteLength);
        return Encoding.UTF8.GetString(span);
    }

    /// <summary>
    ///     读取 LEB128 长度前缀的UTF-8 字符串并前进相应字节数的
    /// </summary>
    /// <returns>解码后的字符串的/returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string read_leb128_string()
    {
        var length = (int)read_leb128_u32();
        return read_string(length);
    }

    /// <summary>
    ///     读取的null 终止的UTF-8 字符串并前进相应字节数（含终止符）的
    /// </summary>
    /// <returns>解码后的字符串的/returns>
    public string read_null_terminated_string()
    {
        var start = position;

        while (position < _data.Length && _data[position] != 0) position++;

        var length = position - start;

        if (position < _data.Length) position++;

        return length == 0 ? string.Empty : Encoding.UTF8.GetString(_data.Slice(start, length));
    }

    #endregion

    #region 指定位置读取

    /// <summary>
    ///     在指定位置读取一个无符号 8 位整数，不移动位置的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte read_u8_at(int offset)
    {
        if (offset < 0 || offset >= _data.Length) return 0;

        return _data[offset];
    }

    /// <summary>
    ///     在指定位置以指定字节序读取一个有符号 32 位整数，不移动位置的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int read_i32_at(int offset, bool bigEndian)
    {
        if (offset < 0 || offset + 4 > _data.Length) return 0;

        return bigEndian
            ? BinaryPrimitives.ReadInt32BigEndian(_data.Slice(offset, 4))
            : BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(offset, 4));
    }

    /// <summary>
    ///     在指定位置以小端序读取一个无符号 32 位整数，不移动位置的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint read_u32_at(int offset)
    {
        if (offset < 0 || offset + 4 > _data.Length) return 0;

        return BinaryPrimitives.ReadUInt32LittleEndian(_data.Slice(offset, 4));
    }

    /// <summary>
    ///     在指定位置以指定字节序读取一�?2 位浮点数，不移动位置�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float read_f32_at(int offset, bool bigEndian)
    {
        if (offset < 0 || offset + 4 > _data.Length) return 0f;

        return bigEndian
            ? BinaryPrimitives.ReadSingleBigEndian(_data.Slice(offset, 4))
            : BinaryPrimitives.ReadSingleLittleEndian(_data.Slice(offset, 4));
    }

    /// <summary>
    ///     在指定位置读取以 null 终止的UTF-8 字符串，不移动位置的
    /// </summary>
    public string read_string_at(int offset)
    {
        if (offset < 0 || offset >= _data.Length) return string.Empty;

        var end = offset;

        while (end < _data.Length && _data[end] != 0) end++;

        return end == offset ? string.Empty : Encoding.UTF8.GetString(_data.Slice(offset, end - offset));
    }

    #endregion

    #region 泛型编解码器读取

    /// <summary>
    ///     使用指定编解码器从当前位置读取值并前进相应字节数的
    /// </summary>
    /// <typeparam name="T">
    ///     读取的值类型的/typeparam>
    ///     <typeparam name="TCodec">
    ///         编解码器类型�?typeparam>
    ///         <param name="codec">
    ///             用于解码的编解码器实例的/param>
    ///             <returns>解码后的值的/returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T read<T, TCodec>(TCodec codec) where TCodec : ICodec<T>
    {
        var size = codec.get_size(default!);
        var value = codec.decode(_data[position..]);
        if (size > 0) position += size;

        return value;
    }

    #endregion

    #region Unsafe 快速读取路�?

    /// <summary>
    ///     不检查边界地读取一个无符号 8 位整数并前进 1 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte unsafe_read_u8()
    {
        return _data[position++];
    }

    /// <summary>
    ///     不检查边界地以小端序读取一个无符号 16 位整数并前进 2 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort unsafe_read_u16_le()
    {
        var value = Unsafe.ReadUnaligned<ushort>(ref MemoryMarshal.GetReference(_data[position..]));
        position += 2;

        if (BitConverter.IsLittleEndian) return value;

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    ///     不检查边界地以大端序读取一个无符号 16 位整数并前进 2 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort unsafe_read_u16_be()
    {
        var value = Unsafe.ReadUnaligned<ushort>(ref MemoryMarshal.GetReference(_data[position..]));
        position += 2;

        if (!BitConverter.IsLittleEndian) return value;

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    ///     不检查边界地以小端序读取一个无符号 32 位整数并前进 4 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint unsafe_read_u32_le()
    {
        var value = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(_data[position..]));
        position += 4;

        if (BitConverter.IsLittleEndian) return value;

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    ///     不检查边界地以大端序读取一个无符号 32 位整数并前进 4 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint unsafe_read_u32_be()
    {
        var value = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(_data[position..]));
        position += 4;

        if (!BitConverter.IsLittleEndian) return value;

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    ///     不检查边界地以小端序读取一个无符号 64 位整数并前进 8 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong unsafe_read_u64_le()
    {
        var value = Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(_data[position..]));
        position += 8;

        if (BitConverter.IsLittleEndian) return value;

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    ///     不检查边界地以大端序读取一个无符号 64 位整数并前进 8 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong unsafe_read_u64_be()
    {
        var value = Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(_data[position..]));
        position += 8;

        if (!BitConverter.IsLittleEndian) return value;

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    ///     不检查边界地以小端序读取一个有符号 32 位整数并前进 4 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int unsafe_read_i32_le()
    {
        return (int)unsafe_read_u32_le();
    }

    /// <summary>
    ///     不检查边界地以大端序读取一个有符号 32 位整数并前进 4 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int unsafe_read_i32_be()
    {
        return (int)unsafe_read_u32_be();
    }

    /// <summary>
    ///     不检查边界地以小端序读取一个有符号 64 位整数并前进 8 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long unsafe_read_i64_le()
    {
        return (long)unsafe_read_u64_le();
    }

    /// <summary>
    ///     不检查边界地以大端序读取一个有符号 64 位整数并前进 8 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long unsafe_read_i64_be()
    {
        return (long)unsafe_read_u64_be();
    }

    /// <summary>
    ///     不检查边界地以小端序读取一�?2 位浮点数并前�? 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float unsafe_read_f32_le()
    {
        return BitConverter.UInt32BitsToSingle(unsafe_read_u32_le());
    }

    /// <summary>
    ///     不检查边界地以大端序读取一�?2 位浮点数并前�? 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float unsafe_read_f32_be()
    {
        return BitConverter.UInt32BitsToSingle(unsafe_read_u32_be());
    }

    /// <summary>
    ///     不检查边界地以小端序读取一�?4 位浮点数并前�? 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double unsafe_read_f64_le()
    {
        return BitConverter.UInt64BitsToDouble(unsafe_read_u64_le());
    }

    /// <summary>
    ///     不检查边界地以大端序读取一�?4 位浮点数并前�? 字节�?    ///     调用方必须保证缓冲区有足够数据的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double unsafe_read_f64_be()
    {
        return BitConverter.UInt64BitsToDouble(unsafe_read_u64_be());
    }

    #endregion
}