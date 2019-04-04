using Std.Data.Binary.Gif.Data;

namespace Std.Data.Binary.Gif.Decode;

/// <summary>
///     GIF 图像解码器——纯 C# 实现，无第三方依的
/// </summary>
/// <remarks>
///     支持 GIF87a 的GIF89a 格式解码，包括多帧动画的
///     全局/局部调色板、LZW 解压、隔行扫描、透明色的
///     图形控制扩展、Netscape 循环扩展的
/// </remarks>
public ref struct GifDecoder
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;

    private int _width;
    private int _height;
    private byte[] _global_color_table;
    private byte _background_color_index;
    private byte _pixel_aspect_ratio;
    private int _loop_count;

    private int _delay_centiseconds;
    private GifDisposalMethod _disposal_method;
    private bool _has_transparent_color;
    private int _transparent_color_index;


    /// <summary>
    ///     初始的GIF 解码的
    /// </summary>
    /// <param name="data">GIF 二进制数的/param>
    public GifDecoder(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
        _global_color_table = [];
    }


    /// <summary>
    ///     解码 GIF 图像为结构化数据
    /// </summary>
    /// <returns>GIF 图像数据。</returns>
    public GifImageData decode()
    {
        parse_header();
        parse_logical_screen_descriptor();

        var frames = new List<GifImageFrame>();

        while (_position < _data.Length)
        {
            var blockType = _data[_position];

            switch (blockType)
            {
                case GifConstants.image_separator:
                    _position++;
                    var frame = parse_image_descriptor();
                    frames.Add(frame);
                    break;
                case GifConstants.extension_introducer:
                    _position++;
                    parse_extension();
                    break;
                case GifConstants.trailer:
                    goto done;
                default:
                    _position++;
                    break;
            }
        }

        done:
        return new GifImageData
        {
            width = _width,
            height = _height,
            global_color_table = _global_color_table,
            background_color_index = _background_color_index,
            pixel_aspect_ratio = _pixel_aspect_ratio,
            loop_count = _loop_count,
            frames = frames
        };
    }


    /// <summary>
    ///     解码 GIF 图像的RGBA 帧列的
    /// </summary>
    /// <returns>RGBA 帧列的/returns>
    public List<GifFrame> decode_to_rgba()
    {
        var gifData = decode();
        var frames = new List<GifFrame>();
        var canvas = new byte[_width * _height * 4];
        var prevCanvas = new byte[_width * _height * 4];

        foreach (var frame in gifData.frames)
        {
            var palette = frame.local_color_table.Length > 0
                ? frame.local_color_table
                : gifData.global_color_table;

            if (frame.disposal_method == GifDisposalMethod.restore_to_previous)
                Array.Copy(canvas, prevCanvas, canvas.Length);

            composite_frame(canvas, frame, palette, gifData);

            var rgbaData = new byte[_width * _height * 4];
            Array.Copy(canvas, rgbaData, canvas.Length);

            frames.Add(new GifFrame
            {
                width = _width,
                height = _height,
                delay_centiseconds = frame.delay_centiseconds,
                disposal_method = frame.disposal_method,
                rgba_data = rgbaData
            });

            switch (frame.disposal_method)
            {
                case GifDisposalMethod.restore_to_background:
                    clear_region(canvas, frame.left, frame.top, frame.width, frame.height);
                    break;
                case GifDisposalMethod.restore_to_previous:
                    Array.Copy(prevCanvas, canvas, canvas.Length);
                    break;
            }
        }

        return frames;
    }

    #region 头部解析

    private void parse_header()
    {
        if (_position + GifConstants.signature_length > _data.Length)
            throw new InvalidDataException("GIF 数据过短，无法读取签名。");

        var sig = _data.Slice(_position, GifConstants.signature_length);
        _position += GifConstants.signature_length;

        if (!sig.SequenceEqual(GifConstants.signature87_a) && !sig.SequenceEqual(GifConstants.signature89_a))
            throw new InvalidDataException("无效的GIF 签名");
    }

    private void parse_logical_screen_descriptor()
    {
        _width = read_u16();
        _height = read_u16();
        var packed = _data[_position++];
        _background_color_index = _data[_position++];
        _pixel_aspect_ratio = _data[_position++];

        var hasGct = (packed & 0x80) != 0;
        if (hasGct)
        {
            var gctSize = 3 * (1 << ((packed & 0x07) + 1));
            _global_color_table = new byte[gctSize];
            _data.Slice(_position, gctSize).CopyTo(_global_color_table);
            _position += gctSize;
        }
    }

    #endregion

    #region 图像描述符解的

    private GifImageFrame parse_image_descriptor()
    {
        var left = read_u16();
        var top = read_u16();
        var width = read_u16();
        var height = read_u16();
        var packed = _data[_position++];

        var hasLct = (packed & 0x80) != 0;
        var interlaced = (packed & 0x40) != 0;

        byte[] localColorTable = [];
        if (hasLct)
        {
            var lctSize = 3 * (1 << ((packed & 0x07) + 1));
            localColorTable = new byte[lctSize];
            _data.Slice(_position, lctSize).CopyTo(localColorTable);
            _position += lctSize;
        }

        var minCodeSize = _data[_position++];
        var compressedData = read_sub_blocks();
        var indices = lzw_decompress(compressedData, minCodeSize, width * height);

        if (interlaced) indices = deinterlace(indices, width, height);

        var frame = new GifImageFrame
        {
            left = left,
            top = top,
            width = width,
            height = height,
            local_color_table = localColorTable,
            delay_centiseconds = _delay_centiseconds,
            disposal_method = _disposal_method,
            has_transparent_color = _has_transparent_color,
            transparent_color_index = _transparent_color_index,
            interlaced = interlaced,
            indices = indices
        };

        _delay_centiseconds = 0;
        _disposal_method = GifDisposalMethod.none;
        _has_transparent_color = false;
        _transparent_color_index = 0;

        return frame;
    }

    #endregion

    #region 扩展块解的

    private void parse_extension()
    {
        if (_position >= _data.Length) return;

        var label = _data[_position++];

        switch (label)
        {
            case GifConstants.graphic_control_label:
                parse_graphic_control_extension();
                break;
            case GifConstants.application_extension_label:
                parse_application_extension();
                break;
            default:
                skip_sub_blocks();
                break;
        }
    }

    private void parse_graphic_control_extension()
    {
        var blockSize = _data[_position++];
        if (blockSize != 4)
        {
            _position += blockSize;
            return;
        }

        var packed = _data[_position++];
        _delay_centiseconds = read_u16();
        _transparent_color_index = _data[_position++];

        _disposal_method = (GifDisposalMethod)((packed >> 2) & 0x07);
        _has_transparent_color = (packed & 0x01) != 0;

        _position++;
    }

    private void parse_application_extension()
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
                _loop_count = read_u16();
                _position += subBlockSize - 3;
            }

            _position++;
        }
        else
        {
            skip_sub_blocks();
        }
    }

    #endregion

    #region LZW 解压

    private static byte[] lzw_decompress(byte[] compressed, int minCodeSize, int totalPixels)
    {
        var clearCode = 1 << minCodeSize;
        var eoiCode = clearCode + 1;
        var codeSize = minCodeSize + 1;
        var nextCode = eoiCode + 1;
        var maxCode = 1 << codeSize;

        var output = new List<byte>(totalPixels);

        var prefixTable = new int[4096];
        var suffixTable = new byte[4096];
        var lengthTable = new int[4096];

        for (var i = 0; i < clearCode; i++)
        {
            prefixTable[i] = -1;
            suffixTable[i] = (byte)i;
            lengthTable[i] = 1;
        }

        var bitReader = new GifLzwBitReader(compressed);

        var code = bitReader.read_code(codeSize);
        if (code != clearCode)
            if (code < clearCode)
                output.Add((byte)code);

        code = bitReader.read_code(codeSize);
        var prevCode = code;

        while (code != eoiCode && output.Count < totalPixels)
        {
            if (code == clearCode)
            {
                codeSize = minCodeSize + 1;
                nextCode = eoiCode + 1;
                maxCode = 1 << codeSize;

                code = bitReader.read_code(codeSize);
                if (code == eoiCode) break;

                if (code < clearCode) output.Add((byte)code);

                prevCode = code;
                code = bitReader.read_code(codeSize);
                continue;
            }

            if (code < nextCode)
            {
                var stack = new List<byte>();
                var c = code;
                while (c >= 0)
                {
                    stack.Add(suffixTable[c]);
                    c = prefixTable[c];
                }

                for (var i = stack.Count - 1; i >= 0; i--) output.Add(stack[i]);
            }
            else
            {
                var stack = new List<byte>();
                var c = prevCode;
                while (c >= 0)
                {
                    stack.Add(suffixTable[c]);
                    c = prefixTable[c];
                }

                var firstByte = stack[^1];
                for (var i = stack.Count - 1; i >= 0; i--) output.Add(stack[i]);
                output.Add(firstByte);
            }

            if (nextCode < 4096)
            {
                prefixTable[nextCode] = prevCode;

                var tempCode = code < nextCode ? code : prevCode;
                while (prefixTable[tempCode] >= 0) tempCode = prefixTable[tempCode];
                suffixTable[nextCode] = suffixTable[tempCode];
                lengthTable[nextCode] = lengthTable[prevCode] + 1;

                nextCode++;
                if (nextCode > maxCode && codeSize < 12)
                {
                    codeSize++;
                    maxCode = 1 << codeSize;
                }
            }

            prevCode = code;
            code = bitReader.read_code(codeSize);
        }

        var result = new byte[totalPixels];
        var copyLen = System.Math.Min(output.Count, totalPixels);
        for (var i = 0; i < copyLen; i++) result[i] = output[i];
        return result;
    }

    #endregion

    #region 隔行扫描

    private static byte[] deinterlace(byte[] indices, int width, int height)
    {
        var result = new byte[indices.Length];
        var passes = new (int Start, int Step)[]
        {
            (0, 8),
            (4, 8),
            (2, 4),
            (1, 2)
        };

        var srcRow = 0;
        foreach (var (start, step) in passes)
            for (var y = start; y < height; y += step)
            {
                var srcOffset = srcRow * width;
                var dstOffset = y * width;
                if (srcOffset + width <= indices.Length) Array.Copy(indices, srcOffset, result, dstOffset, width);
                srcRow++;
            }

        return result;
    }

    #endregion

    #region 帧合的

    private static void composite_frame(byte[] canvas, GifImageFrame frame, byte[] palette, GifImageData gifData)
    {
        for (var y = 0; y < frame.height; y++)
        for (var x = 0; x < frame.width; x++)
        {
            var canvasX = frame.left + x;
            var canvasY = frame.top + y;

            if (canvasX >= gifData.width || canvasY >= gifData.height) continue;

            var srcIdx = y * frame.width + x;
            if (srcIdx >= frame.indices.Length) continue;

            var colorIndex = frame.indices[srcIdx];

            if (frame.has_transparent_color && colorIndex == frame.transparent_color_index) continue;

            var dstIdx = (canvasY * gifData.width + canvasX) * 4;

            var palOffset = colorIndex * 3;
            if (palOffset + 2 < palette.Length)
            {
                canvas[dstIdx] = palette[palOffset];
                canvas[dstIdx + 1] = palette[palOffset + 1];
                canvas[dstIdx + 2] = palette[palOffset + 2];
                canvas[dstIdx + 3] = 255;
            }
        }
    }

    private static void clear_region(byte[] canvas, int left, int top, int width, int height)
    {
        for (var y = top; y < top + height; y++)
        for (var x = left; x < left + width; x++)
        {
            var idx = (y * width + x) * 4;
            if (idx + 3 < canvas.Length)
            {
                canvas[idx] = 0;
                canvas[idx + 1] = 0;
                canvas[idx + 2] = 0;
                canvas[idx + 3] = 0;
            }
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

    private byte[] read_sub_blocks()
    {
        var blocks = new List<byte>();

        while (_position < _data.Length)
        {
            var blockSize = _data[_position++];
            if (blockSize == 0) break;

            if (_position + blockSize > _data.Length) break;

            for (var i = 0; i < blockSize; i++) blocks.Add(_data[_position++]);
        }

        return [.. blocks];
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

internal ref struct GifLzwBitReader
{
    private readonly byte[] _data;
    private int _byte_pos;
    private int _bit_pos;

    public GifLzwBitReader(byte[] data)
    {
        _data = data;
        _byte_pos = 0;
        _bit_pos = 0;
    }

    public int read_code(int codeSize)
    {
        var code = 0;
        for (var i = 0; i < codeSize; i++)
        {
            if (_byte_pos >= _data.Length) return 0;

            var bit = (_data[_byte_pos] >> _bit_pos) & 1;
            code |= bit << i;

            _bit_pos++;
            if (_bit_pos >= 8)
            {
                _bit_pos = 0;
                _byte_pos++;
            }
        }

        return code;
    }
}