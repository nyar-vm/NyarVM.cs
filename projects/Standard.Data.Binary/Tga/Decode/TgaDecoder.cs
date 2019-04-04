using Std.Data.Binary.Tga.Data;

namespace Std.Data.Binary.Tga.Decode;

/// <summary>
///     TGA (Targa) 图像解码器，的TGA 二进制格式解码为 <see cref="TgaImageData" /> 数据结构的
/// </summary>
/// <remarks>
///     支持 TGA 格式的未压缩的RLE 压缩像素数据的
///     支持 8/16/24/32 位色深、调色板、上下翻转的
///     像素数据保留 TGA 原生 BGR/BGRA 顺序，不做色彩空间转换的
/// </remarks>
public ref struct TgaDecoder
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;

    /// <summary>
    ///     初始的<see cref="TgaDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">TGA 二进制数据的/param>
    public TgaDecoder(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }

    /// <summary>
    ///     解码 TGA 图像，保的TGA 原生格式信息的
    /// </summary>
    /// <returns>TGA 图像数据，像素数据为 BGR/BGRA 原生顺序的/returns>
    public TgaImageData decode()
    {
        var idLength = _data[_position++];
        var colorMapType = _data[_position++];
        var imageType = _data[_position++];
        var colorMapFirstEntry = read_u16();
        var colorMapLength = read_u16();
        var colorMapEntrySize = _data[_position++];
        var xOrigin = read_u16();
        var yOrigin = read_u16();
        var width = read_u16();
        var height = read_u16();
        var pixelDepth = _data[_position++];
        var imageDescriptor = _data[_position++];

        var imageId = read_image_id(idLength);

        var hasColorMap = colorMapType == 1;
        var colorMap = read_color_map(hasColorMap, colorMapLength, colorMapEntrySize);

        var isRle = imageType is 9 or 10 or 11;
        var isTopDown = (imageDescriptor & 0x20) != 0;
        var bytesPerPixel = pixelDepth / 8;

        var pixelData = isRle
            ? decode_rle_pixels(width, height, bytesPerPixel)
            : decode_raw_pixels(width, height, bytesPerPixel);

        return new TgaImageData
        {
            width = width,
            height = height,
            pixel_depth = pixelDepth,
            image_type = (TgaImageType)imageType,
            is_top_down = isTopDown,
            has_color_map = hasColorMap,
            color_map = colorMap,
            pixel_data = pixelData,
            id_length = idLength,
            image_id = imageId
        };
    }

    #region 私有方法

    private byte[] read_image_id(int idLength)
    {
        if (idLength == 0) return [];

        var imageId = new byte[idLength];
        _data.Slice(_position, idLength).CopyTo(imageId);
        _position += idLength;
        return imageId;
    }

    private byte[] read_color_map(bool hasColorMap, int length, int entrySize)
    {
        if (!hasColorMap || length == 0) return [];

        var entryBytes = (entrySize + 7) / 8;
        var colorMap = new byte[length * 4];

        for (var i = 0; i < length; i++)
        {
            var r = (byte)0;
            var g = (byte)0;
            var b = (byte)0;
            var a = (byte)255;

            switch (entrySize)
            {
                case 32:
                    b = _data[_position++];
                    g = _data[_position++];
                    r = _data[_position++];
                    a = _data[_position++];
                    break;
                case 24:
                    b = _data[_position++];
                    g = _data[_position++];
                    r = _data[_position++];
                    break;
                case 16:
                    var pixel = _data[_position] | (_data[_position + 1] << 8);
                    _position += 2;
                    r = (byte)(((pixel >> 10) & 0x1F) * 255 / 31);
                    g = (byte)(((pixel >> 5) & 0x1F) * 255 / 31);
                    b = (byte)((pixel & 0x1F) * 255 / 31);
                    a = (pixel & 0x8000) != 0 ? (byte)255 : (byte)0;
                    break;
                default:
                    _position += entryBytes;
                    break;
            }

            var offset = i * 4;
            colorMap[offset] = r;
            colorMap[offset + 1] = g;
            colorMap[offset + 2] = b;
            colorMap[offset + 3] = a;
        }

        return colorMap;
    }

    private byte[] decode_raw_pixels(int width, int height, int bytesPerPixel)
    {
        var totalBytes = width * height * bytesPerPixel;
        var pixels = new byte[totalBytes];
        var count = System.Math.Min(totalBytes, _data.Length - _position);
        _data.Slice(_position, count).CopyTo(pixels);
        _position += totalBytes;
        return pixels;
    }

    private byte[] decode_rle_pixels(int width, int height, int bytesPerPixel)
    {
        var totalPixels = width * height;
        var pixels = new byte[totalPixels * bytesPerPixel];
        var pixelIdx = 0;

        while (pixelIdx < totalPixels)
        {
            var header = _data[_position++];
            var isRun = (header & 0x80) != 0;
            var count = (header & 0x7F) + 1;

            if (isRun)
            {
                var runStart = pixelIdx * bytesPerPixel;
                for (var b = 0; b < bytesPerPixel; b++)
                {
                    var val = _data[_position++];
                    for (var p = 0; p < count; p++) pixels[runStart + p * bytesPerPixel + b] = val;
                }
            }
            else
            {
                var copyBytes = count * bytesPerPixel;
                var srcStart = _position;
                var dstStart = pixelIdx * bytesPerPixel;
                for (var i = 0; i < copyBytes; i++) pixels[dstStart + i] = _data[srcStart + i];

                _position += copyBytes;
            }

            pixelIdx += count;
        }

        return pixels;
    }

    private ushort read_u16()
    {
        var value = (ushort)(_data[_position] | (_data[_position + 1] << 8));
        _position += 2;
        return value;
    }

    #endregion
}