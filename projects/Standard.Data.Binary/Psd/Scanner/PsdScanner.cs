using Std.Data.Binary.Frame;
using Std.Data.Binary.Psd.Data;

namespace Std.Data.Binary.Psd.Scanner;

/// <summary>
///     PSD 文件扫描器，基于 <see cref="SpanScanner" /> 提供的Adobe Photoshop PSD 文件的快速元信息扫描的
/// </summary>
public ref struct PsdScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="PsdScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 PSD 字节数据的/param>
    public PsdScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 PSD 文件头，提取基本图像信息的
    /// </summary>
    public PsdScanHeader scan_header()
    {
        if (_scanner.length < PsdConstants.header_size) throw new InvalidDataException("PSD 文件数据过短，无法读取文件头");

        if (!_scanner.match_magic(PsdConstants.magic_number)) throw new InvalidDataException("PSD 文件魔数不匹配。");

        _scanner.consume_magic(PsdConstants.magic_number);

        var version = _scanner.buffer.read_u16_be();

        _scanner.advance(6);

        var channels = _scanner.buffer.read_u16_be();
        var height = _scanner.buffer.read_u32_be();
        var width = _scanner.buffer.read_u32_be();
        var depth = _scanner.buffer.read_u16_be();
        var colorMode = _scanner.buffer.read_u16_be();

        return new PsdScanHeader
        {
            version = version,
            channels = channels,
            height = (int)height,
            width = (int)width,
            depth = depth,
            color_mode = colorMode
        };
    }

    /// <summary>
    ///     扫描 PSD 文件，提取图层名称列表的
    /// </summary>
    public List<string> scan_layer_names()
    {
        var names = new List<string>();

        if (_scanner.length < PsdConstants.header_size) return names;

        _scanner.consume_magic(PsdConstants.magic_number);
        _scanner.advance(PsdConstants.header_size - 4);

        var colorModeDataLength = _scanner.buffer.read_u32_be();

        if (colorModeDataLength > 0 && _scanner.position + (int)colorModeDataLength <= _scanner.length)
            _scanner.advance((int)colorModeDataLength);
        else if (colorModeDataLength > 0) return names;

        var imageResourcesLength = _scanner.buffer.read_u32_be();

        if (imageResourcesLength > 0 && _scanner.position + (int)imageResourcesLength <= _scanner.length)
            _scanner.advance((int)imageResourcesLength);
        else if (imageResourcesLength > 0) return names;

        var layerAndMaskInfoLength = _scanner.buffer.read_u32_be();

        if (layerAndMaskInfoLength == 0) return names;

        var layerInfoEnd = _scanner.position + (int)layerAndMaskInfoLength;

        var layerInfoLength = _scanner.buffer.read_u32_be();
        var layerCount = _scanner.buffer.read_i16_be();

        if (layerCount < 0) layerCount = (short)-layerCount;

        for (var i = 0; i < layerCount; i++)
        {
            _scanner.advance(16);

            var channelCount = _scanner.buffer.read_u16_be();

            for (var j = 0; j < channelCount; j++) _scanner.advance(6);

            _scanner.advance(12);

            var extraFieldLength = _scanner.buffer.read_u32_be();
            var extraFieldEnd = _scanner.position + (int)extraFieldLength;

            _scanner.advance(8);

            var nameLength = _scanner.buffer.read_u8();

            if (nameLength > 0)
            {
                var name = _scanner.buffer.read_string(nameLength);
                names.Add(name);
            }

            _scanner.position = extraFieldEnd;
        }

        return names;
    }

    /// <summary>
    ///     扫描 PSD 文件，获取图层数量的
    /// </summary>
    public int scan_layer_count()
    {
        if (_scanner.length < PsdConstants.header_size) return 0;

        _scanner.consume_magic(PsdConstants.magic_number);
        _scanner.advance(PsdConstants.header_size - 4);

        var colorModeDataLength = _scanner.buffer.read_u32_be();

        if (colorModeDataLength > 0 && _scanner.position + (int)colorModeDataLength <= _scanner.length)
            _scanner.advance((int)colorModeDataLength);
        else if (colorModeDataLength > 0) return 0;

        var imageResourcesLength = _scanner.buffer.read_u32_be();

        if (imageResourcesLength > 0 && _scanner.position + (int)imageResourcesLength <= _scanner.length)
            _scanner.advance((int)imageResourcesLength);
        else if (imageResourcesLength > 0) return 0;

        var layerAndMaskInfoLength = _scanner.buffer.read_u32_be();

        if (layerAndMaskInfoLength == 0) return 0;

        var layerInfoLength = _scanner.buffer.read_u32_be();
        var layerCount = _scanner.buffer.read_i16_be();

        return layerCount < 0 ? -layerCount : layerCount;
    }
}

/// <summary>
///     PSD 扫描头部信息的
/// </summary>
public sealed class PsdScanHeader
{
    /// <summary>
    ///     PSD 版本号（1 = PSD的 = PSB）的
    /// </summary>
    public ushort version { get; init; }

    /// <summary>
    ///     通道数量的
    /// </summary>
    public ushort channels { get; init; }

    /// <summary>
    ///     图像高度的
    /// </summary>
    public int height { get; init; }

    /// <summary>
    ///     图像宽度的
    /// </summary>
    public int width { get; init; }

    /// <summary>
    ///     位深度的
    /// </summary>
    public ushort depth { get; init; }

    /// <summary>
    ///     颜色模式的
    /// </summary>
    public ushort color_mode { get; init; }

    /// <summary>
    ///     版本名称的
    /// </summary>
    public string version_name => version == 1 ? "PSD" : "PSB";

    /// <summary>
    ///     颜色模式名称的
    /// </summary>
    public string color_mode_name => (PsdColorMode)color_mode switch
    {
        PsdColorMode.bitmap => "位图",
        PsdColorMode.grayscale => "灰度",
        PsdColorMode.indexed => "索引色",
        PsdColorMode.rgb => "RGB",
        PsdColorMode.cmyk => "CMYK",
        PsdColorMode.multichannel => "多通道",
        PsdColorMode.duotone => "双色调",
        PsdColorMode.lab => "Lab",
        _ => "未知"
    };
}