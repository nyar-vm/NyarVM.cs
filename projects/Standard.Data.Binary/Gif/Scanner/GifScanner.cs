using Std.Data.Binary.Gif.Data;

namespace Std.Data.Binary.Gif.Scanner;

/// <summary>
///     GIF 帧扫描器——快速扫的GIF 文件元数据，无需解码像素数据
/// </summary>
/// <remarks>
///     扫描逻辑屏幕描述符、帧位置/尺寸/延迟、调色板信息、循环次数等的
///     不执的LZW 解压，适用于快速获的GIF 文件结构和元信息的
/// </remarks>
public ref struct GifScanner
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;


    /// <summary>
    ///     初始的GIF 扫描的
    /// </summary>
    /// <param name="data">GIF 二进制数的/param>
    public GifScanner(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }


    /// <summary>
    ///     扫描 GIF 文件头部信息
    /// </summary>
    /// <returns>GIF 扫描头部数据。</returns>
    public GifScanHeader scan()
    {
        if (_data.Length < GifConstants.signature_length + GifConstants.logical_screen_descriptor_size)
            return new GifScanHeader { is_valid = false };

        var version = _data.Slice(0, GifConstants.signature_length);
        var isValid = version.SequenceEqual(GifConstants.signature87_a) ||
                      version.SequenceEqual(GifConstants.signature89_a);

        if (!isValid) return new GifScanHeader { is_valid = false };

        _position = GifConstants.signature_length;

        var width = read_u16();
        var height = read_u16();
        var packed = _data[_position++];
        var backgroundColorIndex = _data[_position++];
        var pixelAspectRatio = _data[_position++];

        var hasGct = (packed & 0x80) != 0;
        var gctSizeBits = packed & 0x07;
        var gctSize = hasGct ? 3 * (1 << (gctSizeBits + 1)) : 0;
        _position += gctSize;

        var frames = new List<GifScanFrame>();
        var loopCount = -1;

        while (_position < _data.Length)
        {
            var blockType = _data[_position];

            switch (blockType)
            {
                case GifConstants.image_separator:
                    _position++;
                    var frame = scan_image_descriptor();
                    frames.Add(frame);
                    break;
                case GifConstants.extension_introducer:
                    _position++;
                    scan_extension(ref loopCount);
                    break;
                case GifConstants.trailer:
                    goto done;
                default:
                    _position++;
                    break;
            }
        }

        done:
        return new GifScanHeader
        {
            is_valid = true,
            width = width,
            height = height,
            has_global_color_table = hasGct,
            global_color_table_size_bits = gctSizeBits,
            background_color_index = backgroundColorIndex,
            pixel_aspect_ratio = pixelAspectRatio,
            loop_count = loopCount,
            frames = frames
        };
    }

    #region 图像描述符扫的

    private GifScanFrame scan_image_descriptor()
    {
        var left = read_u16();
        var top = read_u16();
        var width = read_u16();
        var height = read_u16();
        var packed = _data[_position++];

        var hasLct = (packed & 0x80) != 0;
        var interlaced = (packed & 0x40) != 0;
        var lctSizeBits = packed & 0x07;
        var lctSize = hasLct ? 3 * (1 << (lctSizeBits + 1)) : 0;
        _position += lctSize;

        if (_position < _data.Length) _position++;

        skip_sub_blocks();

        return new GifScanFrame
        {
            left = left,
            top = top,
            width = width,
            height = height,
            has_local_color_table = hasLct,
            local_color_table_size_bits = lctSizeBits,
            interlaced = interlaced,
            delay_centiseconds = _scan_delay,
            disposal_method = _scan_disposal,
            has_transparent_color = _scan_has_transparent,
            transparent_color_index = _scan_transparent_index
        };
    }

    #endregion

    #region 扩展块扫的

    private int _scan_delay;
    private GifDisposalMethod _scan_disposal;
    private bool _scan_has_transparent;
    private int _scan_transparent_index;

    private void scan_extension(ref int loopCount)
    {
        if (_position >= _data.Length) return;

        var label = _data[_position++];

        switch (label)
        {
            case GifConstants.graphic_control_label:
                scan_graphic_control_extension();
                break;
            case GifConstants.application_extension_label:
                scan_application_extension(ref loopCount);
                break;
            default:
                skip_sub_blocks();
                break;
        }
    }

    private void scan_graphic_control_extension()
    {
        var blockSize = _data[_position++];
        if (blockSize != 4)
        {
            _position += blockSize;
            if (_position < _data.Length && _data[_position] == 0) _position++;

            return;
        }

        var packed = _data[_position++];
        _scan_delay = read_u16();
        _scan_transparent_index = _data[_position++];
        _scan_disposal = (GifDisposalMethod)((packed >> 2) & 0x07);
        _scan_has_transparent = (packed & 0x01) != 0;

        if (_position < _data.Length && _data[_position] == 0) _position++;
    }

    private void scan_application_extension(ref int loopCount)
    {
        var blockSize = _data[_position++];
        if (blockSize != 11)
        {
            _position += blockSize;
            skip_sub_blocks();
            return;
        }

        var appId = _data.Slice(_position, 11);
        _position += 11;

        if (appId.SequenceEqual(GifConstants.netscape_app_id))
        {
            var subBlockSize = _data[_position++];
            if (subBlockSize >= 3)
            {
                _position++;
                loopCount = read_u16();
                _position += subBlockSize - 3;
            }

            if (_position < _data.Length && _data[_position] == 0) _position++;
        }
        else
        {
            skip_sub_blocks();
        }
    }

    #endregion

    #region 辅助方法

    private int read_u16()
    {
        var value = _data[_position] | (_data[_position + 1] << 8);
        _position += 2;
        return value;
    }

    private void skip_sub_blocks()
    {
        while (_position < _data.Length)
        {
            var blockSize = _data[_position++];
            if (blockSize == 0) break;

            _position += blockSize;
        }
    }

    #endregion
}

/// <summary>
///     GIF 扫描头部数据
/// </summary>
public sealed class GifScanHeader
{
    /// <summary>
    ///     是否为有效的 GIF 文件
    /// </summary>
    public bool is_valid { get; init; }


    /// <summary>
    ///     逻辑屏幕宽度
    /// </summary>
    public int width { get; init; }


    /// <summary>
    ///     逻辑屏幕高度
    /// </summary>
    public int height { get; init; }


    /// <summary>
    ///     是否有全局调色的
    /// </summary>
    public bool has_global_color_table { get; init; }


    /// <summary>
    ///     全局调色板大小位的
    /// </summary>
    public int global_color_table_size_bits { get; init; }


    /// <summary>
    ///     背景色索的
    /// </summary>
    public byte background_color_index { get; init; }


    /// <summary>
    ///     像素宽高的
    /// </summary>
    public byte pixel_aspect_ratio { get; init; }


    /// <summary>
    ///     循环次数的 = 无限循环的1 = 未指定）
    /// </summary>
    public int loop_count { get; init; }


    /// <summary>
    ///     帧扫描信息列的
    /// </summary>
    public IReadOnlyList<GifScanFrame> frames { get; init; } = [];
}

/// <summary>
///     GIF 帧扫描数的
/// </summary>
public sealed class GifScanFrame
{
    /// <summary>
    ///     帧左偏移
    /// </summary>
    public int left { get; init; }


    /// <summary>
    ///     帧上偏移
    /// </summary>
    public int top { get; init; }


    /// <summary>
    ///     帧宽的
    /// </summary>
    public int width { get; init; }


    /// <summary>
    ///     帧高的
    /// </summary>
    public int height { get; init; }


    /// <summary>
    ///     是否有局部调色板
    /// </summary>
    public bool has_local_color_table { get; init; }


    /// <summary>
    ///     局部调色板大小位数
    /// </summary>
    public int local_color_table_size_bits { get; init; }


    /// <summary>
    ///     是否隔行扫描
    /// </summary>
    public bool interlaced { get; init; }


    /// <summary>
    ///     帧延迟时间（1/100 秒）
    /// </summary>
    public int delay_centiseconds { get; init; }


    /// <summary>
    ///     帧处置方的
    /// </summary>
    public GifDisposalMethod disposal_method { get; init; }


    /// <summary>
    ///     是否有透明的
    /// </summary>
    public bool has_transparent_color { get; init; }


    /// <summary>
    ///     透明色索的
    /// </summary>
    public int transparent_color_index { get; init; }
}