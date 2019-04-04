namespace Olympus.Athena.Storage;

/// <summary>
///     列数据块，支持 Plain、Delta、VarInt、Dictionary 四种编码的列式存储
/// </summary>
/// <typeparam name="T">非托管值类型</typeparam>
public sealed class ColumnChunk<T> where T : unmanaged
{
    #region 工厂方法

    /// <summary>
    ///     从已编码的原始数据创建列数据块
    /// </summary>
    /// <param name="data">已编码的数据</param>
    /// <param name="encoding">编码类型</param>
    /// <param name="count">值个数</param>
    /// <returns>列数据块实例</returns>
    public static ColumnChunk<T> FromRaw(ReadOnlySpan<byte> data, EncodingType encoding, int count)
    {
        return new ColumnChunk<T>
        {
            _data = data.ToArray(),
            Encoding = encoding,
            Count = count
        };
    }

    #endregion

    #region 字段

    private Memory<byte> _data;
    private static readonly int ElementSize = Unsafe.SizeOf<T>();

    private static readonly bool IsInt = typeof(T) == typeof(long) || typeof(T) == typeof(ulong)
                                                                   || typeof(T) == typeof(int) ||
                                                                   typeof(T) == typeof(uint)
                                                                   || typeof(T) == typeof(short) ||
                                                                   typeof(T) == typeof(ushort)
                                                                   || typeof(T) == typeof(byte) ||
                                                                   typeof(T) == typeof(sbyte);

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建具有指定初始容量的列数据块
    /// </summary>
    /// <param name="initialCapacity">初始容量（字节数）</param>
    public ColumnChunk(int initialCapacity)
    {
        _data = new byte[initialCapacity];
        Encoding = EncodingType.Plain;
        Count = 0;
    }

    /// <summary>
    ///     创建列数据块并预填充数据
    /// </summary>
    /// <param name="values">初始值集合</param>
    public ColumnChunk(ReadOnlySpan<T> values)
    {
        Count = values.Length;
        Encoding = SelectEncoding(values);
        _data = Encode(values, Encoding);
    }

    private ColumnChunk()
    {
        _data = Array.Empty<byte>();
    }

    #endregion

    #region 属性

    /// <summary>
    ///     当前使用的编码类型
    /// </summary>
    public EncodingType Encoding { get; private set; }

    /// <summary>
    ///     存储的值个数
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    ///     原始编码数据
    /// </summary>
    public ReadOnlyMemory<byte> RawData => _data[..];

    #endregion

    #region 公共方法

    /// <summary>
    ///     解码返回所有值
    /// </summary>
    /// <returns>包含所有解码值的只读跨度</returns>
    public T[] GetValues()
    {
        if (Count == 0) return [];

        return Decode(_data.Span, Encoding, Count);
    }

    /// <summary>
    ///     追加数据并重新编码
    /// </summary>
    /// <param name="values">要追加的值</param>
    public void Append(ReadOnlySpan<T> values)
    {
        if (values.Length == 0) return;

        if (Count == 0)
        {
            Count = values.Length;
            Encoding = SelectEncoding(values);
            _data = Encode(values, Encoding);
            return;
        }

        var existing = GetValues();
        var combined = new T[Count + values.Length];
        existing.CopyTo(combined.AsSpan());
        values.CopyTo(combined.AsSpan(Count));
        Count = combined.Length;
        Encoding = SelectEncoding(combined.AsSpan());
        _data = Encode(combined.AsSpan(), Encoding);
    }

    #endregion

    #region 编码

    private static byte[] Encode(ReadOnlySpan<T> values, EncodingType encoding)
    {
        return encoding switch
        {
            EncodingType.Plain => EncodePlain(values),
            EncodingType.Delta => EncodeDelta(values),
            EncodingType.VarInt => EncodeVarInt(values),
            EncodingType.Dictionary => EncodeDictionary(values),
            _ => throw new AthenaException($"不支持的编码类型：{encoding}")
        };
    }

    private static byte[] EncodePlain(ReadOnlySpan<T> values)
    {
        if (values.Length == 0) return [];

        var bytes = new byte[values.Length * ElementSize];
        MemoryMarshal.AsBytes(values).CopyTo(bytes);
        return bytes;
    }

    private static byte[] EncodeDelta(ReadOnlySpan<T> values)
    {
        if (values.Length == 0) return [];

        var bytes = new byte[values.Length * ElementSize];
        MemoryMarshal.Write(bytes.AsSpan(0, ElementSize), in values[0]);

        for (var i = 1; i < values.Length; i++)
        {
            var delta = Subtract(values[i], values[i - 1]);
            MemoryMarshal.Write(bytes.AsSpan(i * ElementSize, ElementSize), in delta);
        }

        return bytes;
    }

    private static byte[] EncodeVarInt(ReadOnlySpan<T> values)
    {
        if (values.Length == 0) return [];

        using var memStream = new MemoryStream(values.Length * ElementSize);
        for (var i = 0; i < values.Length; i++)
        {
            var raw = ToInt64(values[i]);
            WriteVarInt(memStream, (ulong)ZigZagEncode(raw));
        }

        return memStream.ToArray();
    }

    private static byte[] EncodeDictionary(ReadOnlySpan<T> values)
    {
        if (values.Length == 0) return [];

        var dict = new Dictionary<T, int>();
        var indices = new int[values.Length];
        var uniqueValues = new List<T>();

        for (var i = 0; i < values.Length; i++)
        {
            if (!dict.TryGetValue(values[i], out var index))
            {
                index = uniqueValues.Count;
                dict[values[i]] = index;
                uniqueValues.Add(values[i]);
            }

            indices[i] = index;
        }

        var headerSize = sizeof(int);
        var dictSize = uniqueValues.Count * ElementSize;
        var indexSize = indices.Length * sizeof(int);
        var totalSize = headerSize + dictSize + indexSize;
        var bytes = new byte[totalSize];

        BitConverter.TryWriteBytes(bytes.AsSpan(0, sizeof(int)), uniqueValues.Count);
        var dictBytes = MemoryMarshal.AsBytes(uniqueValues.ToArray().AsSpan());
        dictBytes.CopyTo(bytes.AsSpan(headerSize, dictSize));
        MemoryMarshal.AsBytes(indices.AsSpan()).CopyTo(bytes.AsSpan(headerSize + dictSize, indexSize));

        return bytes;
    }

    #endregion

    #region 解码

    private static T[] Decode(ReadOnlySpan<byte> data, EncodingType encoding, int count)
    {
        return encoding switch
        {
            EncodingType.Plain => DecodePlain(data, count),
            EncodingType.Delta => DecodeDelta(data, count),
            EncodingType.VarInt => DecodeVarInt(data, count),
            EncodingType.Dictionary => DecodeDictionary(data, count),
            _ => throw new AthenaException($"不支持的编码类型：{encoding}")
        };
    }

    private static T[] DecodePlain(ReadOnlySpan<byte> data, int count)
    {
        var result = new T[count];
        var src = MemoryMarshal.Cast<byte, T>(data);
        src[..count].CopyTo(result);
        return result;
    }

    private static T[] DecodeDelta(ReadOnlySpan<byte> data, int count)
    {
        var result = new T[count];
        var src = MemoryMarshal.Cast<byte, T>(data);
        result[0] = src[0];

        for (var i = 1; i < count; i++) result[i] = Add(result[i - 1], src[i]);

        return result;
    }

    private static T[] DecodeVarInt(ReadOnlySpan<byte> data, int count)
    {
        var result = new T[count];
        var offset = 0;

        for (var i = 0; i < count; i++)
        {
            var (value, consumed) = ReadVarInt(data[offset..]);
            offset += consumed;
            result[i] = FromInt64(ZigZagDecode((long)value));
        }

        return result;
    }

    private static T[] DecodeDictionary(ReadOnlySpan<byte> data, int count)
    {
        var uniqueCount = BitConverter.ToInt32(data);
        var dictOffset = sizeof(int);
        var dictSize = uniqueCount * ElementSize;
        var dictValues = MemoryMarshal.Cast<byte, T>(data.Slice(dictOffset, dictSize)).ToArray();

        var indexOffset = dictOffset + dictSize;
        var indices = MemoryMarshal.Cast<byte, int>(data.Slice(indexOffset, count * sizeof(int)));

        var result = new T[count];
        for (var i = 0; i < count; i++) result[i] = dictValues[indices[i]];

        return result;
    }

    #endregion

    #region 编码选择

    private static EncodingType SelectEncoding(ReadOnlySpan<T> values)
    {
        if (values.Length <= 1) return EncodingType.Plain;

        if (ShouldUseDictionary(values)) return EncodingType.Dictionary;

        if (ShouldUseDelta(values)) return EncodingType.Delta;

        if (IsInt && ShouldUseVarInt(values)) return EncodingType.VarInt;

        return EncodingType.Plain;
    }

    private static bool ShouldUseDictionary(ReadOnlySpan<T> values)
    {
        if (values.Length < 4) return false;

        var set = new HashSet<T>();
        for (var i = 0; i < values.Length; i++)
        {
            set.Add(values[i]);

            if (set.Count > values.Length * 0.3) return false;
        }

        return set.Count > 1 && set.Count < values.Length * 0.5;
    }

    private static bool ShouldUseDelta(ReadOnlySpan<T> values)
    {
        if (values.Length < 2) return false;

        var elementBits = ElementSize * 8;
        var threshold = 1UL << (elementBits / 2);

        for (var i = 1; i < values.Length; i++)
        {
            var delta = ToUInt64(Subtract(values[i], values[i - 1]));
            if (delta >= threshold && (long)delta >= 0) return false;
        }

        return true;
    }

    private static bool ShouldUseVarInt(ReadOnlySpan<T> values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            var raw = ToInt64(values[i]);
            var zigzag = ZigZagEncode(raw);
            if (zigzag >= 1L << 56) return false;
        }

        return true;
    }

    #endregion

    #region 算术辅助

    private static T Add(T left, T right)
    {
        if (typeof(T) == typeof(long)) return (T)(object)((long)(object)left + (long)(object)right);

        if (typeof(T) == typeof(ulong)) return (T)(object)((ulong)(object)left + (ulong)(object)right);

        if (typeof(T) == typeof(int)) return (T)(object)((int)(object)left + (int)(object)right);

        if (typeof(T) == typeof(uint)) return (T)(object)((uint)(object)left + (uint)(object)right);

        if (typeof(T) == typeof(short)) return (T)(object)(short)((short)(object)left + (short)(object)right);

        if (typeof(T) == typeof(ushort)) return (T)(object)(ushort)((ushort)(object)left + (ushort)(object)right);

        if (typeof(T) == typeof(byte)) return (T)(object)(byte)((byte)(object)left + (byte)(object)right);

        if (typeof(T) == typeof(sbyte)) return (T)(object)(sbyte)((sbyte)(object)left + (sbyte)(object)right);

        if (typeof(T) == typeof(float)) return (T)(object)((float)(object)left + (float)(object)right);

        if (typeof(T) == typeof(double)) return (T)(object)((double)(object)left + (double)(object)right);

        throw new AthenaException($"类型 {typeof(T).Name} 不支持算术运算");
    }

    private static T Subtract(T left, T right)
    {
        if (typeof(T) == typeof(long)) return (T)(object)((long)(object)left - (long)(object)right);

        if (typeof(T) == typeof(ulong)) return (T)(object)((ulong)(object)left - (ulong)(object)right);

        if (typeof(T) == typeof(int)) return (T)(object)((int)(object)left - (int)(object)right);

        if (typeof(T) == typeof(uint)) return (T)(object)((uint)(object)left - (uint)(object)right);

        if (typeof(T) == typeof(short)) return (T)(object)(short)((short)(object)left - (short)(object)right);

        if (typeof(T) == typeof(ushort)) return (T)(object)(ushort)((ushort)(object)left - (ushort)(object)right);

        if (typeof(T) == typeof(byte)) return (T)(object)(byte)((byte)(object)left - (byte)(object)right);

        if (typeof(T) == typeof(sbyte)) return (T)(object)(sbyte)((sbyte)(object)left - (sbyte)(object)right);

        if (typeof(T) == typeof(float)) return (T)(object)((float)(object)left - (float)(object)right);

        if (typeof(T) == typeof(double)) return (T)(object)((double)(object)left - (double)(object)right);

        throw new AthenaException($"类型 {typeof(T).Name} 不支持算术运算");
    }

    #endregion

    #region VarInt 辅助

    private static long ZigZagEncode(long value)
    {
        return (value << 1) ^ (value >> 63);
    }

    private static long ZigZagDecode(long value)
    {
        return (long)((ulong)value >> 1) ^ -(value & 1);
    }

    private static void WriteVarInt(Stream stream, ulong value)
    {
        while (value >= 0x80)
        {
            stream.WriteByte((byte)(value | 0x80));
            value >>= 7;
        }

        stream.WriteByte((byte)value);
    }

    private static (ulong Value, int BytesConsumed) ReadVarInt(ReadOnlySpan<byte> data)
    {
        ulong result = 0;
        var shift = 0;

        for (var i = 0; i < data.Length; i++)
        {
            var b = data[i];
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) return (result, i + 1);

            shift += 7;
        }

        throw new AthenaException("VarInt 数据格式错误：未找到终止字节");
    }

    #endregion

    #region 类型转换辅助

    private static long ToInt64(T value)
    {
        if (typeof(T) == typeof(long)) return (long)(object)value;

        if (typeof(T) == typeof(ulong)) return (long)(ulong)(object)value;

        if (typeof(T) == typeof(int)) return (int)(object)value;

        if (typeof(T) == typeof(uint)) return (uint)(object)value;

        if (typeof(T) == typeof(short)) return (short)(object)value;

        if (typeof(T) == typeof(ushort)) return (ushort)(object)value;

        if (typeof(T) == typeof(byte)) return (byte)(object)value;

        if (typeof(T) == typeof(sbyte)) return (sbyte)(object)value;

        throw new AthenaException($"类型 {typeof(T).Name} 不支持 VarInt 编码");
    }

    private static ulong ToUInt64(T value)
    {
        if (typeof(T) == typeof(long)) return (ulong)(long)(object)value;

        if (typeof(T) == typeof(ulong)) return (ulong)(object)value;

        if (typeof(T) == typeof(int)) return (ulong)(int)(object)value;

        if (typeof(T) == typeof(uint)) return (uint)(object)value;

        if (typeof(T) == typeof(short)) return (ulong)(short)(object)value;

        if (typeof(T) == typeof(ushort)) return (ushort)(object)value;

        if (typeof(T) == typeof(byte)) return (byte)(object)value;

        if (typeof(T) == typeof(sbyte)) return (ulong)(sbyte)(object)value;

        if (typeof(T) == typeof(float)) return (ulong)BitConverter.SingleToInt32Bits((float)(object)value);

        if (typeof(T) == typeof(double)) return (ulong)BitConverter.DoubleToInt64Bits((double)(object)value);

        return 0;
    }

    private static T FromInt64(long value)
    {
        if (typeof(T) == typeof(long)) return (T)(object)value;

        if (typeof(T) == typeof(ulong)) return (T)(object)(ulong)value;

        if (typeof(T) == typeof(int)) return (T)(object)(int)value;

        if (typeof(T) == typeof(uint)) return (T)(object)(uint)value;

        if (typeof(T) == typeof(short)) return (T)(object)(short)value;

        if (typeof(T) == typeof(ushort)) return (T)(object)(ushort)value;

        if (typeof(T) == typeof(byte)) return (T)(object)(byte)value;

        if (typeof(T) == typeof(sbyte)) return (T)(object)(sbyte)value;

        throw new AthenaException($"类型 {typeof(T).Name} 不支持从 Int64 转换");
    }

    #endregion
}