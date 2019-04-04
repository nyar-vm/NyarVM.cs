namespace Std.Codec;

#region LEB128 变长整数编解码器

/// <summary>
///     LEB128 (Little Endian Base 128) 无符号 32 位整数编解码器。
///     这是 VarInt 算法的一种具体实现，广泛用于 DWARF、WebAssembly、Protobuf 等格式。
/// </summary>
public readonly struct Leb128UInt32 : ICodec<uint>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int get_size(uint value)
    {
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void encode(uint value, Span<byte> destination)
    {
        var pos = 0;
        while (true)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0) b |= 0x80;

            destination[pos++] = b;
            if (value == 0) break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint decode(ReadOnlySpan<byte> source)
    {
        uint result = 0;
        var shift = 0;
        var pos = 0;
        while (true)
        {
            var b = source[pos++];
            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;

            shift += 7;
        }

        return result;
    }
}

/// <summary>
///     LEB128 (Little Endian Base 128) 无符号 64 位整数编解码器。
///     这是 VarInt 算法的一种具体实现，广泛用于 DWARF、WebAssembly、Protobuf 等格式。
/// </summary>
public readonly struct Leb128UInt64 : ICodec<ulong>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int get_size(ulong value)
    {
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void encode(ulong value, Span<byte> destination)
    {
        var pos = 0;
        while (true)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0) b |= 0x80;

            destination[pos++] = b;
            if (value == 0) break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong decode(ReadOnlySpan<byte> source)
    {
        ulong result = 0;
        var shift = 0;
        var pos = 0;
        while (true)
        {
            var b = source[pos++];
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;

            shift += 7;
        }

        return result;
    }
}

/// <summary>
///     LEB128 (Little Endian Base 128) 有符号 32 位整数编解码器。
///     这是 VarInt 算法的一种具体实现，使用符号扩展编码有符号整数。
/// </summary>
public readonly struct Leb128Int32 : ICodec<int>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int get_size(int value)
    {
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void encode(int value, Span<byte> destination)
    {
        var pos = 0;
        var more = true;
        while (more)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;

            if ((value == 0 && (b & 0x40) == 0) || (value == -1 && (b & 0x40) != 0))
                more = false;
            else
                b |= 0x80;

            destination[pos++] = b;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int decode(ReadOnlySpan<byte> source)
    {
        var result = 0;
        var shift = 0;
        var pos = 0;
        byte b;

        do
        {
            b = source[pos++];
            result |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        if (shift < 32 && (b & 0x40) != 0) result |= ~0 << shift;

        return result;
    }
}

/// <summary>
///     LEB128 (Little Endian Base 128) 有符号 64 位整数编解码器。
///     这是 VarInt 算法的一种具体实现，使用符号扩展编码有符号整数。
/// </summary>
public readonly struct Leb128Int64 : ICodec<long>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int get_size(long value)
    {
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void encode(long value, Span<byte> destination)
    {
        var pos = 0;
        var more = true;
        while (more)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;

            if ((value == 0 && (b & 0x40) == 0) || (value == -1 && (b & 0x40) != 0))
                more = false;
            else
                b |= 0x80;

            destination[pos++] = b;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long decode(ReadOnlySpan<byte> source)
    {
        long result = 0;
        var shift = 0;
        var pos = 0;
        byte b;

        do
        {
            b = source[pos++];
            result |= (long)(b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        if (shift < 64 && (b & 0x40) != 0) result |= ~0L << shift;

        return result;
    }
}

#endregion

#region ZigZag + LEB128 编解码器

/// <summary>
///     ZigZag + LEB128 编码的 32 位整数编解码器。
///     ZigZag 将有符号整数映射为无符号整数，使小绝对值的负数也能用较少字节编码。
///     广泛用于 Protobuf 等格式。
/// </summary>
public readonly struct ZigZagLeb128Int32 : ICodec<int>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int get_size(int value)
    {
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void encode(int value, Span<byte> destination)
    {
        var zigzag = (uint)((value << 1) ^ (value >> 31));
        var pos = 0;
        while (true)
        {
            var b = (byte)(zigzag & 0x7F);
            zigzag >>= 7;
            if (zigzag != 0) b |= 0x80;

            destination[pos++] = b;
            if (zigzag == 0) break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int decode(ReadOnlySpan<byte> source)
    {
        uint raw = 0;
        var shift = 0;
        var pos = 0;
        while (true)
        {
            var b = source[pos++];
            raw |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;

            shift += 7;
        }

        return (int)((raw >> 1) ^ (0u - (raw & 1)));
    }
}

/// <summary>
///     ZigZag + LEB128 编码的 64 位整数编解码器。
///     ZigZag 将有符号整数映射为无符号整数，使小绝对值的负数也能用较少字节编码。
///     广泛用于 Protobuf 等格式。
/// </summary>
public readonly struct ZigZagLeb128Int64 : ICodec<long>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int get_size(long value)
    {
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void encode(long value, Span<byte> destination)
    {
        var zigzag = (ulong)((value << 1) ^ (value >> 63));
        var pos = 0;
        while (true)
        {
            var b = (byte)(zigzag & 0x7F);
            zigzag >>= 7;
            if (zigzag != 0) b |= 0x80;

            destination[pos++] = b;
            if (zigzag == 0) break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long decode(ReadOnlySpan<byte> source)
    {
        ulong raw = 0;
        var shift = 0;
        var pos = 0;
        while (true)
        {
            var b = source[pos++];
            raw |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;

            shift += 7;
        }

        return (long)((raw >> 1) ^ (0UL - (raw & 1)));
    }
}

#endregion