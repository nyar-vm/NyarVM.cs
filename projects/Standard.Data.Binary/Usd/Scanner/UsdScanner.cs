using Std.Data.Binary.Frame;
using Std.Data.Binary.Usd.Data;

namespace Std.Data.Binary.Usd.Scanner;

/// <summary>
///     USD 扫描器的
/// </summary>
public ref struct UsdScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="UsdScanner" /> 结构的新实例的
    /// </summary>
    public UsdScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     扫描 USD 文件头的
    /// </summary>
    public UsdScanHeader scan_header()
    {
        if (_scanner.length < UsdConstants.header_size) return new UsdScanHeader { file_type = UsdFileType.unknown };

        if (_scanner.match_magic(UsdConstants.usdc_magic))
        {
            _scanner.consume_magic(UsdConstants.usdc_magic);
            var version = (int)_scanner.buffer.read_u32_le();

            return new UsdScanHeader
            {
                file_type = UsdFileType.crate,
                version = version
            };
        }

        return new UsdScanHeader { file_type = UsdFileType.unknown };
    }

    /// <summary>
    ///     快速判断是否为 USDC 格式的
    /// </summary>
    public bool is_usdc()
    {
        return _scanner.length >= UsdConstants.magic_length && _scanner.match_magic(UsdConstants.usdc_magic);
    }
}

/// <summary>
///     USD 扫描头部信息的
/// </summary>
public sealed class UsdScanHeader
{
    /// <summary>
    ///     文件类型的
    /// </summary>
    public UsdFileType file_type { get; init; }

    /// <summary>
    ///     版本号的
    /// </summary>
    public int version { get; init; }
}