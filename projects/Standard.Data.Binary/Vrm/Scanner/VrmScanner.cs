using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Vrm.Scanner;

/// <summary>
///     VRM 扫描器的
/// </summary>
public ref struct VrmScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="VrmScanner" /> 结构的新实例的
    /// </summary>
    public VrmScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     扫描 VRM 文件头的
    /// </summary>
    public VrmScanHeader scan_header()
    {
        if (_scanner.length < 4) return new VrmScanHeader();

        var magic = _scanner.buffer.read_string(4);

        if (magic != "glTF") return new VrmScanHeader();

        return new VrmScanHeader { is_gltf = true };
    }

    /// <summary>
    ///     是否可能的VRM 文件（基的glTF 魔数）的
    /// </summary>
    public bool is_possible_vrm()
    {
        if (_scanner.length < 4) return false;

        var magic = _scanner.buffer.read_string(4);
        _scanner.position = 0;
        return magic == "glTF";
    }
}

/// <summary>
///     VRM 扫描头部信息的
/// </summary>
public sealed class VrmScanHeader
{
    /// <summary>
    ///     是否的glTF 容器的
    /// </summary>
    public bool is_gltf { get; init; }
}