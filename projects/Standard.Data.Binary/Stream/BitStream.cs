using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Std.Data.Binary.Stream;

/// <summary>
///     比特流结构体，支持按位读写数据，最小化网络传输数据量。
/// </summary>
/// <remarks>
///     <para>
///         比特流适用于需要将数据压缩到比特级别的场景，例如网络同步、紧凑存储等。
///         写入时自动扩容，读取时按比特位解析。
///     </para>
///     <para>
///         内部采用 LSB-first 比特排列：比特 0 位于字节 0 的最低位。
///         字节对齐时使用直接写入/读取，非对齐时按字节块处理，
///         相比逐位循环可获得最高 8 倍的吞吐提升。
///     </para>
/// </remarks>
public ref struct BitStream
{
    #region 字段

    private byte[] _buffer;

    #endregion

    #region 属性

    /// <summary>
    ///     获取底层缓冲区的只读视图。
    /// </summary>
    public ReadOnlySpan<byte> buffer => new(_buffer, 0, byte_length);

    /// <summary>
    ///     获取当前比特位置。
    /// </summary>
    public int bit_position { get; private set; }

    /// <summary>
    ///     获取比特总长度。
    /// </summary>
    public int bit_length { get; private set; }

    /// <summary>
    ///     获取字节总长度（向上取整）。
    /// </summary>
    public int byte_length => (bit_length + 7) >> 3;

    /// <summary>
    ///     获取剩余可读比特数。
    /// </summary>
    public int remaining_bits => bit_length - bit_position;

    /// <summary>
    ///     获取是否已到达末尾。
    /// </summary>
    public bool is_end => bit_position >= bit_length;

    #endregion

    #region 构造函数

    /// <summary>
    ///     初始化写入用比特流。
    /// </summary>
    /// <param name="capacity">初始容量（字节）。</param>
    public BitStream(int capacity)
    {
        _buffer = new byte[capacity];
        bit_position = 0;
        bit_length = 0;
    }

    /// <summary>
    ///     初始化读取用比特流。
    /// </summary>
    /// <param name="data">要读取的数据。</param>
    public BitStream(ReadOnlySpan<byte> data)
    {
        _buffer = new byte[data.Length];
        data.CopyTo(_buffer);
        bit_position = 0;
        bit_length = data.Length * 8;
    }

    #endregion

    #region 写入方法

    /// <summary>
    ///     写入单个比特。
    /// </summary>
    /// <param name="value">比特值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_bit(bool value)
    {
        ensure_write_capacity(1);

        var byteIndex = bit_position >> 3;
        var bitIndex = bit_position & 7;

        if (value)
            _buffer[byteIndex] |= (byte)(1 << bitIndex);
        else
            _buffer[byteIndex] &= (byte)~(1 << bitIndex);

        bit_position++;
        bit_length = System.Math.Max(bit_length, bit_position);
    }

    /// <summary>
    ///     写入指定比特数的无符号整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    /// <param name="bits">比特数（1-32）。</param>
    public void write_bits(uint value, int bits)
    {
        if (bits is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(bits), $"比特数必须在 1-32 之间，当前值：{bits}");

        ensure_write_capacity(bits);

        var bitOffset = bit_position & 7;

        if (bitOffset == 0)
            write_aligned(value, bits);
        else
            write_unaligned(value, bits, bitOffset);

        bit_position += bits;
        bit_length = System.Math.Max(bit_length, bit_position);
    }

    /// <summary>
    ///     字节对齐快速写入路径。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void write_aligned(uint value, int bits)
    {
        var byteIndex = bit_position >> 3;
        var fullBytes = bits >> 3;

        for (var i = 0; i < fullBytes; i++) _buffer[byteIndex + i] = (byte)(value >> (i * 8));

        var remaining = bits & 7;

        if (remaining > 0) _buffer[byteIndex + fullBytes] = (byte)(value >> (fullBytes * 8));
    }

    /// <summary>
    ///     非对齐写入路径：按字节块处理，每次写入当前字节剩余空间。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void write_unaligned(uint value, int bits, int bitOffset)
    {
        var byteIndex = bit_position >> 3;
        var available = 8 - bitOffset;
        var written = 0;

        while (written < bits)
        {
            var toWrite = System.Math.Min(bits - written, available);
            var mask = (1 << toWrite) - 1;
            var bitsToWrite = (int)(value >> written) & mask;

            _buffer[byteIndex] = (byte)((_buffer[byteIndex] & ~(mask << bitOffset)) | (bitsToWrite << bitOffset));

            written += toWrite;
            byteIndex++;
            bitOffset = 0;
            available = 8;
        }
    }

    /// <summary>
    ///     写入字节。
    /// </summary>
    /// <param name="value">字节值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_byte(byte value)
    {
        ensure_write_capacity(8);

        var bitOffset = bit_position & 7;
        var byteIndex = bit_position >> 3;

        if (bitOffset == 0)
        {
            _buffer[byteIndex] = value;
        }
        else
        {
            _buffer[byteIndex] |= (byte)(value << bitOffset);
            _buffer[byteIndex + 1] = (byte)(value >> (8 - bitOffset));
        }

        bit_position += 8;
        bit_length = System.Math.Max(bit_length, bit_position);
    }

    /// <summary>
    ///     写入 16 位无符号整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u_int16(ushort value)
    {
        write_bits(value, 16);
    }

    /// <summary>
    ///     写入 32 位无符号整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u_int32(uint value)
    {
        ensure_write_capacity(32);

        var bitOffset = bit_position & 7;
        var byteIndex = bit_position >> 3;

        if (bitOffset == 0)
        {
            _buffer[byteIndex] = (byte)value;
            _buffer[byteIndex + 1] = (byte)(value >> 8);
            _buffer[byteIndex + 2] = (byte)(value >> 16);
            _buffer[byteIndex + 3] = (byte)(value >> 24);
        }
        else
        {
            write_unaligned(value, 32, bitOffset);
        }

        bit_position += 32;
        bit_length = System.Math.Max(bit_length, bit_position);
    }

    /// <summary>
    ///     写入 64 位无符号整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_u_int64(ulong value)
    {
        ensure_write_capacity(64);

        var bitOffset = bit_position & 7;
        var byteIndex = bit_position >> 3;

        if (bitOffset == 0)
        {
            _buffer[byteIndex] = (byte)value;
            _buffer[byteIndex + 1] = (byte)(value >> 8);
            _buffer[byteIndex + 2] = (byte)(value >> 16);
            _buffer[byteIndex + 3] = (byte)(value >> 24);
            _buffer[byteIndex + 4] = (byte)(value >> 32);
            _buffer[byteIndex + 5] = (byte)(value >> 40);
            _buffer[byteIndex + 6] = (byte)(value >> 48);
            _buffer[byteIndex + 7] = (byte)(value >> 56);
        }
        else
        {
            write_unaligned((uint)(value & 0xFFFFFFFF), 32, bitOffset);
            write_unaligned((uint)(value >> 32), 32, 0);
        }

        bit_position += 64;
        bit_length = System.Math.Max(bit_length, bit_position);
    }

    /// <summary>
    ///     写入有符号 32 位整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_int32(int value)
    {
        write_bits((uint)value, 32);
    }

    /// <summary>
    ///     写入有符号 64 位整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void write_int64(long value)
    {
        write_u_int64((ulong)value);
    }

    /// <summary>
    ///     写入 32 位浮点数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void write_float(float value)
    {
        var bits = BitConverter.SingleToUInt32Bits(value);
        write_bits(bits, 32);
    }

    /// <summary>
    ///     写入 64 位浮点数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void write_double(double value)
    {
        var bits = BitConverter.DoubleToUInt64Bits(value);
        write_u_int64(bits);
    }

    /// <summary>
    ///     写入布尔值（1 比特）。
    /// </summary>
    /// <param name="value">布尔值。</param>
    public void write_bool(bool value)
    {
        write_bit(value);
    }

    /// <summary>
    ///     写入指定范围内的整数，使用最小比特数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    /// <param name="min">最小值（含）。</param>
    /// <param name="max">最大值（含）。</param>
    public void write_ranged_int(int value, int min, int max)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(nameof(value), $"值 {value} 不在范围 [{min}, {max}] 内");

        var range = (uint)(max - min);
        var bits = bits_required(range);
        write_bits((uint)(value - min), bits);
    }

    /// <summary>
    ///     写入指定范围内的浮点数，量化为指定比特数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    /// <param name="min">最小值。</param>
    /// <param name="max">最大值。</param>
    /// <param name="bits">量化比特数。</param>
    public void write_ranged_float(float value, float min, float max, int bits)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(nameof(value), $"值 {value} 不在范围 [{min}, {max}] 内");

        var normalized = (value - min) / (max - min);
        var maxQuantized = (1u << bits) - 1;
        var quantized = (uint)(normalized * maxQuantized + 0.5f);
        write_bits(quantized, bits);
    }

    /// <summary>
    ///     写入字节数组。
    /// </summary>
    /// <param name="data">要写入的数据。</param>
    public void write_bytes(ReadOnlySpan<byte> data)
    {
        write_bits((uint)data.Length, 16);

        if (data.Length == 0) return;

        ensure_write_capacity(data.Length * 8);

        var bitOffset = bit_position & 7;

        if (bitOffset == 0)
        {
            var byteIndex = bit_position >> 3;
            data.CopyTo(new Span<byte>(_buffer, byteIndex, data.Length));
            bit_position += data.Length * 8;
            bit_length = System.Math.Max(bit_length, bit_position);
        }
        else
        {
            for (var i = 0; i < data.Length; i++) write_byte(data[i]);
        }
    }

    /// <summary>
    ///     写入字符串（UTF-8 编码）。
    /// </summary>
    /// <param name="value">要写入的字符串。</param>
    public void write_string(string value)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        var bytes = maxByteCount <= 256
            ? ArrayPool<byte>.Shared.Rent(maxByteCount)
            : new byte[maxByteCount];

        try
        {
            var written = Encoding.UTF8.GetBytes(value, bytes);
            write_bytes(new ReadOnlySpan<byte>(bytes, 0, written));
        }
        finally
        {
            if (maxByteCount <= 256) ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    #endregion

    #region 读取方法

    /// <summary>
    ///     读取单个比特。
    /// </summary>
    /// <returns>比特值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool read_bit()
    {
        if (bit_position >= bit_length) throw new InvalidOperationException("已到达比特流末尾，无法读取更多数据");

        var byteIndex = bit_position >> 3;
        var bitIndex = bit_position & 7;
        var bit = (_buffer[byteIndex] >> bitIndex) & 1;
        bit_position++;

        return bit != 0;
    }

    /// <summary>
    ///     读取指定比特数的无符号整数。
    /// </summary>
    /// <param name="bits">比特数（1-32）。</param>
    /// <returns>读取的无符号整数。</returns>
    public uint read_bits(int bits)
    {
        if (bits is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(bits), $"比特数必须在 1-32 之间，当前值：{bits}");

        if (bit_position + bits > bit_length)
            throw new InvalidOperationException($"比特流剩余 {bit_length - bit_position} 比特，不足以读取 {bits} 比特");

        var bitOffset = bit_position & 7;

        uint value;

        if (bitOffset == 0)
            value = read_aligned(bits);
        else
            value = read_unaligned(bits, bitOffset);

        bit_position += bits;
        return value;
    }

    /// <summary>
    ///     字节对齐快速读取路径。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint read_aligned(int bits)
    {
        var byteIndex = bit_position >> 3;
        var fullBytes = bits >> 3;
        uint value = 0;

        for (var i = 0; i < fullBytes; i++) value |= (uint)_buffer[byteIndex + i] << (i * 8);

        var remaining = bits & 7;

        if (remaining > 0) value |= (uint)(_buffer[byteIndex + fullBytes] & ((1 << remaining) - 1)) << (fullBytes * 8);

        return value;
    }

    /// <summary>
    ///     非对齐读取路径：按字节块处理，每次读取当前字节可用比特。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint read_unaligned(int bits, int bitOffset)
    {
        var byteIndex = bit_position >> 3;
        var available = 8 - bitOffset;
        var read = 0;
        uint value = 0;

        while (read < bits)
        {
            var toRead = System.Math.Min(bits - read, available);
            var mask = (1 << toRead) - 1;
            var byteValue = (_buffer[byteIndex] >> bitOffset) & mask;
            value |= (uint)byteValue << read;

            read += toRead;
            byteIndex++;
            bitOffset = 0;
            available = 8;
        }

        return value;
    }

    /// <summary>
    ///     读取字节。
    /// </summary>
    /// <returns>读取的字节值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte read_byte()
    {
        if (bit_position + 8 > bit_length) throw new InvalidOperationException("比特流剩余比特不足以读取 1 字节");

        var bitOffset = bit_position & 7;
        var byteIndex = bit_position >> 3;
        byte value;

        if (bitOffset == 0)
            value = _buffer[byteIndex];
        else
            value = (byte)((_buffer[byteIndex] >> bitOffset) | (_buffer[byteIndex + 1] << (8 - bitOffset)));

        bit_position += 8;
        return value;
    }

    /// <summary>
    ///     读取 16 位无符号整数。
    /// </summary>
    /// <returns>读取的值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort read_u_int16()
    {
        return (ushort)read_bits(16);
    }

    /// <summary>
    ///     读取 32 位无符号整数。
    /// </summary>
    /// <returns>读取的值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint read_u_int32()
    {
        if (bit_position + 32 > bit_length) throw new InvalidOperationException("比特流剩余比特不足以读取 32 位整数");

        var bitOffset = bit_position & 7;
        var byteIndex = bit_position >> 3;
        uint value;

        if (bitOffset == 0)
            value = _buffer[byteIndex]
                    | ((uint)_buffer[byteIndex + 1] << 8)
                    | ((uint)_buffer[byteIndex + 2] << 16)
                    | ((uint)_buffer[byteIndex + 3] << 24);
        else
            value = read_unaligned(32, bitOffset);

        bit_position += 32;
        return value;
    }

    /// <summary>
    ///     读取 64 位无符号整数。
    /// </summary>
    /// <returns>读取的值。</returns>
    public ulong read_u_int64()
    {
        var low = read_bits(32);
        var high = read_bits(32);
        return ((ulong)high << 32) | low;
    }

    /// <summary>
    ///     读取有符号 32 位整数。
    /// </summary>
    /// <returns>读取的值。</returns>
    public int read_int32()
    {
        return (int)read_bits(32);
    }

    /// <summary>
    ///     读取有符号 64 位整数。
    /// </summary>
    /// <returns>读取的值。</returns>
    public long read_int64()
    {
        return (long)read_u_int64();
    }

    /// <summary>
    ///     读取 32 位浮点数。
    /// </summary>
    /// <returns>读取的值。</returns>
    public float read_float()
    {
        var bits = read_bits(32);
        return BitConverter.UInt32BitsToSingle(bits);
    }

    /// <summary>
    ///     读取 64 位浮点数。
    /// </summary>
    /// <returns>读取的值。</returns>
    public double read_double()
    {
        var bits = read_u_int64();
        return BitConverter.UInt64BitsToDouble(bits);
    }

    /// <summary>
    ///     读取布尔值。
    /// </summary>
    /// <returns>读取的布尔值。</returns>
    public bool read_bool()
    {
        return read_bit();
    }

    /// <summary>
    ///     读取指定范围内的整数。
    /// </summary>
    /// <param name="min">最小值（含）。</param>
    /// <param name="max">最大值（含）。</param>
    /// <returns>读取的值。</returns>
    public int read_ranged_int(int min, int max)
    {
        var range = (uint)(max - min);
        var bits = bits_required(range);
        var raw = read_bits(bits);
        return (int)raw + min;
    }

    /// <summary>
    ///     读取指定范围内的量化浮点数。
    /// </summary>
    /// <param name="min">最小值。</param>
    /// <param name="max">最大值。</param>
    /// <param name="bits">量化比特数。</param>
    /// <returns>读取的浮点值。</returns>
    public float read_ranged_float(float min, float max, int bits)
    {
        var maxQuantized = (1u << bits) - 1;
        var quantized = read_bits(bits);
        var normalized = (float)quantized / maxQuantized;
        return min + normalized * (max - min);
    }

    /// <summary>
    ///     读取字节数组。
    /// </summary>
    /// <returns>读取的字节数组。</returns>
    public byte[] read_bytes()
    {
        var length = (int)read_bits(16);

        if (length == 0) return [];

        var data = new byte[length];

        if ((bit_position & 7) == 0)
        {
            var byteIndex = bit_position >> 3;
            new Span<byte>(_buffer, byteIndex, length).CopyTo(data);
            bit_position += length * 8;
        }
        else
        {
            for (var i = 0; i < length; i++) data[i] = read_byte();
        }

        return data;
    }

    /// <summary>
    ///     读取字符串（UTF-8 编码）。
    /// </summary>
    /// <returns>读取的字符串。</returns>
    public string read_string()
    {
        var bytes = read_bytes();
        return Encoding.UTF8.GetString(bytes);
    }

    #endregion

    #region 公共方法

    /// <summary>
    ///     重置读取位置到起始。
    /// </summary>
    public void reset()
    {
        bit_position = 0;
    }

    /// <summary>
    ///     将比特流数据复制到新数组。
    /// </summary>
    /// <returns>包含比特流数据的字节数组。</returns>
    public byte[] to_array()
    {
        var result = new byte[byte_length];
        Array.Copy(_buffer, result, byte_length);
        return result;
    }

    /// <summary>
    ///     对齐到下一个字节边界。
    /// </summary>
    public void align_to_byte()
    {
        var remainder = bit_position & 7;

        if (remainder != 0)
        {
            bit_position += 8 - remainder;
            bit_length = System.Math.Max(bit_length, bit_position);
        }
    }

    /// <summary>
    ///     计算表示指定值所需的最小比特数。
    /// </summary>
    /// <param name="value">要表示的最大值。</param>
    /// <returns>所需比特数。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int bits_required(uint value)
    {
        if (value == 0) return 1;

        return BitOperations.Log2(value) + 1;
    }

    #endregion

    #region 私有方法

    /// <summary>
    ///     确保写入容量足够。
    /// </summary>
    /// <param name="bitsToWrite">需要写入的比特数。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ensure_write_capacity(int bitsToWrite)
    {
        var requiredBytes = (bit_position + bitsToWrite + 7) >> 3;

        if (requiredBytes <= _buffer.Length) return;

        var newCapacity = System.Math.Max(_buffer.Length * 2, requiredBytes);
        var newBuffer = new byte[newCapacity];
        Array.Copy(_buffer, newBuffer, _buffer.Length);
        _buffer = newBuffer;
    }

    #endregion
}