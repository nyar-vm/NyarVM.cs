using Std.Data.Binary.Frame;

namespace Std.Data.Binary.FlatBuffers.Scanner;

/// <summary>
///     FlatBuffers 扫描器的
/// </summary>
public ref struct FlatBuffersScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="FlatBuffersScanner" /> 结构的新实例的
    /// </summary>
    public FlatBuffersScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     扫描 FlatBuffer 头部的
    /// </summary>
    public FlatBuffersScanHeader scan_header()
    {
        if (_scanner.length < 8) return new FlatBuffersScanHeader();

        var rootOffset = _scanner.buffer.read_u32_le();
        var fileId = _scanner.buffer.read_string(4);

        return new FlatBuffersScanHeader
        {
            root_offset = rootOffset,
            file_identifier = fileId
        };
    }
}

/// <summary>
///     FlatBuffers 扫描头部信息的
/// </summary>
public sealed class FlatBuffersScanHeader
{
    /// <summary>
    ///     根表偏移的
    /// </summary>
    public uint root_offset { get; init; }

    /// <summary>
    ///     文件标识符的
    /// </summary>
    public string file_identifier { get; init; } = string.Empty;
}