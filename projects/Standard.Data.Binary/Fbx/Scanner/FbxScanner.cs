using Std.Data.Binary.Fbx.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Fbx.Scanner;

/// <summary>
///     FBX 文件扫描器，基于 <see cref="SpanScanner" /> 提供的Autodesk FBX 文件的快速元信息扫描的
/// </summary>
/// <remarks>
///     FBX 文件格式由文件头和节点层级结构组成的
///     扫描器只读取文件头和顶层节点信息，不做完整解码，以实现快速探查的
/// </remarks>
public ref struct FbxScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="FbxScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 FBX 字节数据的/param>
    public FbxScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 FBX 文件头，提取版本信息的
    /// </summary>
    /// <returns>FBX 文件头信息的/returns>
    public FbxScanHeader scan_header()
    {
        if (_scanner.length < FbxConstants.header_size) throw new InvalidDataException("FBX 文件数据过短，无法读取文件头");

        var magic = _scanner.buffer.read_string(FbxConstants.magic_length);

        if (!magic.StartsWith("Kaydara FBX Binary")) throw new InvalidDataException("FBX 文件签名不匹配。");

        var version = (int)_scanner.buffer.read_u32_le();

        return new FbxScanHeader
        {
            version = version,
            major_version = version / 1000,
            is_binary = true
        };
    }

    /// <summary>
    ///     快速判断数据是否为 FBX 二进制格式的
    /// </summary>
    public bool is_fbx_binary()
    {
        if (_scanner.length < 19) return false;

        var header = _scanner.data[..19];
        return header.SequenceEqual("Kaydara FBX Binary"u8);
    }
}

/// <summary>
///     FBX 扫描头部信息的
/// </summary>
public sealed class FbxScanHeader
{
    /// <summary>
    ///     FBX 版本号的
    /// </summary>
    public int version { get; init; }

    /// <summary>
    ///     主版本号的
    /// </summary>
    public int major_version { get; init; }

    /// <summary>
    ///     是否为二进制格式的
    /// </summary>
    public bool is_binary { get; init; }

    /// <summary>
    ///     版本名称的
    /// </summary>
    public string version_name => version switch
    {
        FbxVersions.v61 => "FBX 6.1",
        FbxVersions.v70 => "FBX 7.0",
        FbxVersions.v71 => "FBX 7.1",
        FbxVersions.v72 => "FBX 7.2",
        FbxVersions.v73 => "FBX 7.3",
        FbxVersions.v74 => "FBX 7.4",
        FbxVersions.v75 => "FBX 7.5",
        FbxVersions.v77 => "FBX 7.7",
        _ => $"FBX {version / 1000}.{version % 1000 / 100}"
    };
}