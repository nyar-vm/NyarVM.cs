using Std.Data.Binary.Frame;
using Std.Data.Binary.Psd.Data;

namespace Std.Data.Binary.Psd.Decode;

/// <summary>
///     PSD 文件解码器，的Adobe Photoshop 文档格式解码的C# 数据结构的
/// </summary>
/// <remarks>
///     PSD 的Adobe Photoshop 的原生文件格式，支持图层、通道、蒙版等高级图像编辑功能的
///     解码器解的PSD 文件头、颜色模式数据、图像资源、图层和蒙版信息以及图像数据的
/// </remarks>
public ref struct PsdDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="PsdDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">PSD 二进制数据的/param>
    public PsdDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置的
    /// </summary>
    public int position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    /// <summary>
    ///     解码 PSD 文件的
    /// </summary>
    /// <returns>PSD 图像数据的/returns>
    public PsdImageData decode()
    {
        var (width, height, channels, depth, colorMode) = read_file_header();
        skip_color_mode_data();
        skip_image_resources();
        var layers = read_layer_and_mask_info();
        var mergedImageData = read_image_data();

        return new PsdImageData
        {
            width = (int)width,
            height = (int)height,
            channels = channels,
            depth = depth,
            color_mode = colorMode,
            layers = layers,
            merged_image_data = mergedImageData
        };
    }

    /// <summary>
    ///     仅解的PSD 文件头信息的
    /// </summary>
    /// <returns>包含宽度、高度、通道数、深度和颜色模式的元组的/returns>
    public (int Width, int Height, int Channels, int Depth, int ColorMode) decode_header()
    {
        var (width, height, channels, depth, colorMode) = read_file_header();
        return ((int)width, (int)height, channels, depth, colorMode);
    }

    #region 私有解析方法

    private (uint width, uint height, int channels, int depth, int colorMode) read_file_header()
    {
        var signature = _buffer.read_string(4);

        if (signature != "8BPS") throw new InvalidDataException($"PSD 文件签名无效，期的\"8BPS\"，实的\"{signature}\"");

        var version = _buffer.read_u16_be();

        _buffer.advance(6);

        var channels = _buffer.read_u16_be();
        var height = _buffer.read_u32_be();
        var width = _buffer.read_u32_be();
        var depth = _buffer.read_u16_be();
        var colorMode = _buffer.read_u16_be();

        return (width, height, channels, depth, colorMode);
    }

    private void skip_color_mode_data()
    {
        var length = _buffer.read_u32_be();

        if (length > 0) _buffer.advance((int)length);
    }

    private void skip_image_resources()
    {
        var sectionLength = _buffer.read_u32_be();

        if (sectionLength > 0) _buffer.advance((int)sectionLength);
    }

    private List<PsdLayer> read_layer_and_mask_info()
    {
        var layers = new List<PsdLayer>();
        var infoLength = _buffer.read_u32_be();

        if (infoLength == 0) return layers;

        var infoEnd = _buffer.position + (int)infoLength;

        var layerInfoLength = _buffer.read_u32_be();
        var layerInfoEnd = _buffer.position + (int)layerInfoLength;

        if (layerInfoLength > 0)
        {
            var layerCount = _buffer.read_i16_be();

            for (var i = 0; i < System.Math.Abs(layerCount); i++) layers.Add(read_layer());

            for (var i = 0; i < layers.Count; i++) skip_channel_image_data(layers[i]);
        }

        _buffer.position = infoEnd;

        return layers;
    }

    private PsdLayer read_layer()
    {
        var top = _buffer.read_i32_be();
        var left = _buffer.read_i32_be();
        var bottom = _buffer.read_i32_be();
        var right = _buffer.read_i32_be();
        var channelCount = _buffer.read_u16_be();

        var channelDataLengths = new uint[channelCount];

        for (var i = 0; i < channelCount; i++)
        {
            _buffer.advance(2);
            channelDataLengths[i] = _buffer.read_u32_be();
        }

        var blendModeSignature = _buffer.read_string(4);

        if (blendModeSignature != "8BIM")
            throw new InvalidDataException($"PSD 图层混合模式签名无效，期的\"8BIM\"，实的\"{blendModeSignature}\"");

        var blendMode = _buffer.read_string(4);
        var blendModeEnum = parse_blend_mode(blendMode);
        var opacity = _buffer.read_u8();
        var clipping = _buffer.read_u8();
        var flags = _buffer.read_u8();
        _buffer.advance(1);

        var isVisible = (flags & 0x02) == 0;

        var extraDataLength = _buffer.read_u32_be();
        var extraDataEnd = _buffer.position + (int)extraDataLength;

        if (_buffer.position + 4 <= extraDataEnd)
        {
            var layerMaskDataLength = _buffer.read_u32_be();
            if (layerMaskDataLength > 0 && _buffer.position + (int)layerMaskDataLength <= extraDataEnd)
                _buffer.advance((int)layerMaskDataLength);
        }

        if (_buffer.position + 4 <= extraDataEnd)
        {
            var blendingRangesLength = _buffer.read_u32_be();
            if (blendingRangesLength > 0 && _buffer.position + (int)blendingRangesLength <= extraDataEnd)
                _buffer.advance((int)blendingRangesLength);
        }

        var name = read_pascal_string();

        _buffer.position = extraDataEnd;

        return new PsdLayer
        {
            name = name,
            bounds = (top, left, bottom, right),
            channel_count = channelCount,
            channel_data_lengths = channelDataLengths,
            blend_mode = blendMode,
            blend_mode_enum = blendModeEnum,
            opacity = opacity,
            is_visible = isVisible
        };
    }

    private void skip_channel_image_data(PsdLayer layer)
    {
        for (var i = 0; i < layer.channel_count; i++)
            if (i < layer.channel_data_lengths.Count)
            {
                var dataLength = (int)layer.channel_data_lengths[i];

                if (dataLength > 0) _buffer.advance(dataLength);
            }
    }

    private byte[] read_image_data()
    {
        var compression = _buffer.read_u16_be();

        if (_buffer.is_end) return [];

        return [.. _buffer.read_bytes(_buffer.remaining)];
    }

    private string read_pascal_string()
    {
        var length = _buffer.read_u8();

        if (length == 0)
        {
            if ((_buffer.position & 1) == 1) _buffer.advance(1);

            return string.Empty;
        }

        var str = _buffer.read_string(length);

        if ((length + 1) % 2 != 0) _buffer.advance(1);

        return str;
    }

    private static PsdBlendMode parse_blend_mode(string blendMode)
    {
        return blendMode switch
        {
            "norm" => PsdBlendMode.normal,
            "diss" => PsdBlendMode.dissolve,
            "mul " => PsdBlendMode.multiply,
            "scrn" => PsdBlendMode.screen,
            "over" => PsdBlendMode.overlay,
            "sLit" => PsdBlendMode.soft_light,
            "hLit" => PsdBlendMode.hard_light,
            "cLit" => PsdBlendMode.color_dodge,
            "cBurn" => PsdBlendMode.color_burn,
            "dkCl" => PsdBlendMode.darken,
            "lgCl" => PsdBlendMode.lighten,
            "diff" => PsdBlendMode.difference,
            "smud" => PsdBlendMode.exclusion,
            "hue " => PsdBlendMode.hue,
            "sat " => PsdBlendMode.saturation,
            "colr" => PsdBlendMode.color,
            "lum " => PsdBlendMode.luminosity,
            "pass" => PsdBlendMode.pass_through,
            _ => PsdBlendMode.unknown
        };
    }

    #endregion
}