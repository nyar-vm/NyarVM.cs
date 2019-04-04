using Std.Data.Binary.BmFont.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.BmFont.Decode;

/// <summary>
///     BMFont 二进制文件解码器，将 AngelCode BMFont 格式解码的C# 数据结构的
/// </summary>
/// <remarks>
///     BMFont 的AngelCode 的位图字体格式，广泛用于游戏 UI 渲染的
///     解码器支的BMFont 二进制格式（.fnt），文本格式的Oak.BmFont 处理的
/// </remarks>
public ref struct BmFontDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="BmFontDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">BMFont 二进制数据的/param>
    public BmFontDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 BMFont 二进制文件的
    /// </summary>
    /// <returns>BMFont 数据的/returns>
    public BmFontData decode()
    {
        var magic = _buffer.read_string(3);

        if (magic != "BMF") throw new InvalidDataException($"BMFont 文件签名无效，期的\"BMF\"，实的\"{magic}\"");

        var version = _buffer.read_u8();

        if (version != BmFontConstants.binary_version)
            throw new InvalidDataException($"BMFont 版本号无效，期望 {BmFontConstants.binary_version}，实的{version}");

        BmFontInfo? info = null;
        BmFontCommon? common = null;
        var pages = new List<string>();
        var chars = new List<BmFontChar>();
        var kerningPairs = new List<BmFontKerningPair>();

        while (!_buffer.is_end)
        {
            if (_buffer.remaining < 5) break;

            var blockType = _buffer.read_u8();
            var blockSize = (int)_buffer.read_u32_le();

            if (_buffer.remaining < blockSize) break;

            switch (blockType)
            {
                case BmFontConstants.block_info:
                    info = read_info_block(blockSize);
                    break;
                case BmFontConstants.block_common:
                    common = read_common_block();
                    break;
                case BmFontConstants.block_pages:
                    pages = read_pages_block(blockSize, common?.pages ?? 1);
                    break;
                case BmFontConstants.block_chars:
                    chars = read_chars_block(blockSize);
                    break;
                case BmFontConstants.block_kerning_pairs:
                    kerningPairs = read_kerning_block(blockSize);
                    break;
                default:
                    _buffer.advance(blockSize);
                    break;
            }
        }

        return new BmFontData
        {
            info = info ?? new BmFontInfo(),
            common = common ?? new BmFontCommon(),
            pages = pages,
            chars = chars,
            kerning_pairs = kerningPairs
        };
    }

    #region 私有解析方法

    private BmFontInfo read_info_block(int blockSize)
    {
        var size = _buffer.read_i16_le();
        var flags = _buffer.read_u8();
        var bitDepth = _buffer.read_u8();
        var charSet = _buffer.read_u8();

        _buffer.advance(2);

        var spacingH = _buffer.read_i16_le();
        var spacingV = _buffer.read_i16_le();
        var lineHeight = _buffer.read_i16_le();

        var nameLength = blockSize - 13;
        var fontName = nameLength > 0 ? _buffer.read_string(nameLength).TrimEnd('\0') : string.Empty;

        return new BmFontInfo
        {
            size = size,
            bold = (flags & 0x01) != 0,
            italic = (flags & 0x02) != 0,
            unicode = (flags & 0x04) != 0,
            bit_depth = bitDepth,
            char_set = charSet,
            spacing_h = spacingH,
            spacing_v = spacingV,
            line_height = lineHeight,
            font_name = fontName
        };
    }

    private BmFontCommon read_common_block()
    {
        var lineHeight = _buffer.read_u16_le();
        var @base = _buffer.read_u16_le();
        var scaleW = _buffer.read_u16_le();
        var scaleH = _buffer.read_u16_le();
        var pages = _buffer.read_u16_le();
        var flags = _buffer.read_u8();

        _buffer.advance(4);

        return new BmFontCommon
        {
            line_height = lineHeight,
            @base = @base,
            scale_w = scaleW,
            scale_h = scaleH,
            pages = pages,
            alpha_channel = (flags & 0x01) != 0,
            red_channel = (flags & 0x02) != 0,
            green_channel = (flags & 0x04) != 0,
            blue_channel = (flags & 0x08) != 0,
            packed = (flags & 0x10) != 0
        };
    }

    private List<string> read_pages_block(int blockSize, ushort pageCount)
    {
        var pages = new List<string>();
        var pageNameLength = pageCount > 0 ? blockSize / pageCount : 0;

        for (var i = 0; i < pageCount; i++)
        {
            var name = _buffer.read_string(pageNameLength).TrimEnd('\0');
            pages.Add(name);
        }

        return pages;
    }

    private List<BmFontChar> read_chars_block(int blockSize)
    {
        var charSize = 20;
        var count = blockSize / charSize;
        var chars = new List<BmFontChar>(count);

        for (var i = 0; i < count; i++)
        {
            var id = _buffer.read_u32_le();
            var x = _buffer.read_u16_le();
            var y = _buffer.read_u16_le();
            var width = _buffer.read_u16_le();
            var height = _buffer.read_u16_le();
            var xOffset = _buffer.read_i16_le();
            var yOffset = _buffer.read_i16_le();
            var xAdvance = _buffer.read_i16_le();
            var page = _buffer.read_u8();
            var channel = _buffer.read_u8();

            chars.Add(new BmFontChar
            {
                id = id,
                x = x,
                y = y,
                width = width,
                height = height,
                x_offset = xOffset,
                y_offset = yOffset,
                x_advance = xAdvance,
                page = page,
                channel = (BmFontChannel)channel
            });
        }

        return chars;
    }

    private List<BmFontKerningPair> read_kerning_block(int blockSize)
    {
        var pairSize = 10;
        var count = blockSize / pairSize;
        var pairs = new List<BmFontKerningPair>(count);

        for (var i = 0; i < count; i++)
        {
            var first = _buffer.read_u32_le();
            var second = _buffer.read_u32_le();
            var amount = _buffer.read_i16_le();

            pairs.Add(new BmFontKerningPair
            {
                first = first,
                second = second,
                amount = amount
            });
        }

        return pairs;
    }

    #endregion
}