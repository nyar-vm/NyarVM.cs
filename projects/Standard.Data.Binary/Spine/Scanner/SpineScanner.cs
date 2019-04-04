using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Spine.Scanner;

/// <summary>
///     Spine 格式扫描器的默认实现，基的<see cref="SpanScanner" /> 提供零分配的快速数据扫描的
/// </summary>
public ref struct SpineScanner : ISpineScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="SpineScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 Spine 二进制数据的/param>
    public SpineScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <inheritdoc />
    public string read_hash()
    {
        var hashLength = _scanner.buffer.read_u8();

        if (hashLength == 0) return string.Empty;

        return _scanner.buffer.read_string(hashLength);
    }

    /// <inheritdoc />
    public string read_version()
    {
        var versionLength = _scanner.buffer.read_u8();

        if (versionLength == 0) return string.Empty;

        return _scanner.buffer.read_string(versionLength);
    }

    /// <inheritdoc />
    public string? read_spine_string()
    {
        var length = _scanner.buffer.read_leb128_i32();

        if (length <= 0) return null;

        return _scanner.buffer.read_string(length);
    }

    /// <inheritdoc />
    public bool read_spine_boolean()
    {
        return _scanner.buffer.read_u8() != 0;
    }

    /// <inheritdoc />
    public float read_spine_float()
    {
        return _scanner.buffer.read_f32_le();
    }

    /// <inheritdoc />
    public (byte R, byte G, byte B, byte A) read_spine_color()
    {
        var r = _scanner.buffer.read_u8();
        var g = _scanner.buffer.read_u8();
        var b = _scanner.buffer.read_u8();
        var a = _scanner.buffer.read_u8();
        return (r, g, b, a);
    }

    /// <summary>
    ///     读取一个无符号 8 位整数并前进 1 字节的
    /// </summary>
    /// <returns>无符的8 位整数值的/returns>
    public byte read_u_int8()
    {
        return _scanner.buffer.read_u8();
    }

    /// <summary>
    ///     以小端序读取一个无符号 32 位整数并前进 4 字节的
    /// </summary>
    /// <returns>无符的32 位整数值的/returns>
    public uint read_u_int32_little_endian()
    {
        return _scanner.buffer.read_u32_le();
    }

    /// <summary>
    ///     读取一的LEB128 编码的有符号 32 位整数的
    /// </summary>
    /// <returns>解码后的有符的32 位整数值的/returns>
    public int read_leb128_int32()
    {
        return _scanner.buffer.read_leb128_i32();
    }
}