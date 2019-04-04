using Std.Data.Binary.Exr.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Exr.Scanner;

/// <summary>
///     EXR 扫描器的
/// </summary>
public ref struct ExrScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="ExrScanner" /> 结构的新实例的
    /// </summary>
    public ExrScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     扫描 EXR 文件头的
    /// </summary>
    public ExrScanHeader scan_header()
    {
        if (_scanner.length < ExrConstants.header_size) return new ExrScanHeader();

        if (!_scanner.match_magic(ExrConstants.magic_number)) return new ExrScanHeader();

        _scanner.consume_magic(ExrConstants.magic_number);
        var version = _scanner.buffer.read_u32_le();

        return new ExrScanHeader
        {
            version = version,
            major_version = (int)(version & 0xFF),
            is_multipart = (version & 0x200) != 0,
            is_single_part = (version & 0x200) == 0
        };
    }

    /// <summary>
    ///     是否的EXR 格式的
    /// </summary>
    public bool is_exr()
    {
        return _scanner.length >= 4 && _scanner.match_magic(ExrConstants.magic_number);
    }
}

/// <summary>
///     EXR 扫描头部信息的
/// </summary>
public sealed class ExrScanHeader
{
    /// <summary>
    ///     版本号的
    /// </summary>
    public uint version { get; init; }

    /// <summary>
    ///     主版本号的
    /// </summary>
    public int major_version { get; init; }

    /// <summary>
    ///     是否为多部分文件的
    /// </summary>
    public bool is_multipart { get; init; }

    /// <summary>
    ///     是否为单部分文件的
    /// </summary>
    public bool is_single_part { get; init; }
}