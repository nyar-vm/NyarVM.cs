using Std.Data.Binary.Basis.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Basis.Scanner;

/// <summary>
///     Basis/KTX2 扫描器的
/// </summary>
public ref struct BasisScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="BasisScanner" /> 结构的新实例的
    /// </summary>
    public BasisScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     扫描头部信息的
    /// </summary>
    public BasisScanHeader scan_header()
    {
        if (_scanner.length < 12) return new BasisScanHeader();

        if (_scanner.match_magic(BasisConstants.ktx2_magic)) return new BasisScanHeader { format = "KTX2" };

        if (_scanner.length >= 2 && _scanner.data[..2].SequenceEqual(BasisConstants.basis_magic))
            return new BasisScanHeader { format = "Basis" };

        return new BasisScanHeader();
    }

    /// <summary>
    ///     是否的Basis/KTX2 格式的
    /// </summary>
    public bool is_basis_or_ktx2()
    {
        if (_scanner.length >= 12 && _scanner.match_magic(BasisConstants.ktx2_magic)) return true;

        if (_scanner.length >= 2 && _scanner.data[..2].SequenceEqual(BasisConstants.basis_magic)) return true;

        return false;
    }
}

/// <summary>
///     Basis 扫描头部信息的
/// </summary>
public sealed class BasisScanHeader
{
    /// <summary>
    ///     格式名称的
    /// </summary>
    public string format { get; init; } = "未知";
}