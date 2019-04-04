using Core.Media;
using Std.Data.Binary.Bmp.Data;
using Std.Data.Binary.Gif.Data;
using Std.Data.Binary.Png.Data;
using Std.Image.Pixels;
using Std.Media;

namespace Std.Image.Interop;

/// <summary>
///     Acorn 数据映射器，负责 Sonic.Standard.Image 类型与 Acorn 数据模型之间的转换。
/// </summary>
public static class AcornDataMapper
{
    /// <summary>
    ///     将 PNG 图像数据转换为 RGBA32 图像。
    /// </summary>
    /// <param name="png">PNG 图像数据。</param>
    /// <returns>RGBA32 图像。</returns>
    public static Image<Rgba32> to_rgba32(PngImageData png)
    {
        var width = png.width;
        var height = png.height;
        var result = new Image<Rgba32>(width, height, PixelFormat.rgba32);

        var rawSpan = png.raw_pixel_data.AsSpan();
        var bpp = png.bytes_per_pixel;
        var stride = width * bpp + 1;

        for (var y = 0; y < height; y++)
        {
            var rowOffset = y * stride;
            var filterType = rawSpan[rowOffset];

            for (var x = 0; x < width; x++)
            {
                var pixelOffset = rowOffset + 1 + x * bpp;
                var r = (byte)0;
                var g = (byte)0;
                var b = (byte)0;
                var a = (byte)255;

                switch (png.color_type)
                {
                    case PngColorType.truecolor_alpha:
                        r = rawSpan[pixelOffset];
                        g = rawSpan[pixelOffset + 1];
                        b = rawSpan[pixelOffset + 2];
                        a = rawSpan[pixelOffset + 3];
                        break;
                    case PngColorType.truecolor:
                        r = rawSpan[pixelOffset];
                        g = rawSpan[pixelOffset + 1];
                        b = rawSpan[pixelOffset + 2];
                        break;
                    case PngColorType.grayscale_alpha:
                        r = g = b = rawSpan[pixelOffset];
                        a = rawSpan[pixelOffset + 1];
                        break;
                    case PngColorType.grayscale:
                        r = g = b = rawSpan[pixelOffset];
                        break;
                    case PngColorType.indexed:
                        var index = rawSpan[pixelOffset];
                        if (png.palette.Length > 0 && index * 3 + 2 < png.palette.Length)
                        {
                            r = png.palette[index * 3];
                            g = png.palette[index * 3 + 1];
                            b = png.palette[index * 3 + 2];
                        }

                        if (png.transparency.Length > 0 && index < png.transparency.Length) a = png.transparency[index];

                        break;
                }

                result[x, y] = new Rgba32(r, g, b, a);
            }
        }

        return result;
    }

    /// <summary>
    ///     将 BMP 图像数据转换为 RGBA32 图像。
    /// </summary>
    /// <param name="bmp">BMP 图像数据。</param>
    /// <returns>RGBA32 图像。</returns>
    public static Image<Rgba32> to_rgba32(BmpImageData bmp)
    {
        var width = bmp.width;
        var height = bmp.absolute_height;
        var result = new Image<Rgba32>(width, height, PixelFormat.rgba32);

        var pixelSpan = bmp.pixel_data.AsSpan();
        var stride = bmp.stride;

        for (var srcY = 0; srcY < height; srcY++)
        {
            var dstY = bmp.is_top_down ? srcY : height - 1 - srcY;
            var rowOffset = srcY * stride;

            for (var x = 0; x < width; x++)
            {
                var r = (byte)0;
                var g = (byte)0;
                var b = (byte)0;
                var a = (byte)255;

                switch (bmp.bits_per_pixel)
                {
                    case 32:
                    {
                        var offset = rowOffset + x * 4;
                        if (offset + 3 < pixelSpan.Length)
                        {
                            b = pixelSpan[offset];
                            g = pixelSpan[offset + 1];
                            r = pixelSpan[offset + 2];
                            a = pixelSpan[offset + 3];
                        }

                        break;
                    }
                    case 24:
                    {
                        var offset = rowOffset + x * 3;
                        if (offset + 2 < pixelSpan.Length)
                        {
                            b = pixelSpan[offset];
                            g = pixelSpan[offset + 1];
                            r = pixelSpan[offset + 2];
                        }

                        break;
                    }
                    case 8:
                    {
                        var palIdx = pixelSpan[rowOffset + x];
                        if (palIdx < bmp.palette.Length)
                        {
                            var color = bmp.palette[palIdx];
                            b = (byte)(color & 0xFF);
                            g = (byte)((color >> 8) & 0xFF);
                            r = (byte)((color >> 16) & 0xFF);
                            a = (byte)((color >> 24) & 0xFF);
                        }

                        break;
                    }
                }

                result[x, dstY] = new Rgba32(r, g, b, a);
            }
        }

        return result;
    }

    /// <summary>
    ///     将 GIF 图像数据转换为动画帧数组。
    /// </summary>
    /// <param name="gif">GIF 图像数据。</param>
    /// <returns>动画帧数组。</returns>
    public static AnimationFrame<Rgba32>[] to_gif_frames(GifImageData gif)
    {
        var gifFrames = DecodeGifToRgba(gif);

        var frames = new AnimationFrame<Rgba32>[gifFrames.Count];

        for (var i = 0; i < gifFrames.Count; i++)
        {
            var gifFrame = gifFrames[i];
            var image = new Image<Rgba32>(gifFrame.width, gifFrame.height, PixelFormat.rgba32);

            for (var y = 0; y < gifFrame.height; y++)
            for (var x = 0; x < gifFrame.width; x++)
            {
                var offset = (y * gifFrame.width + x) * 4;

                if (offset + 3 < gifFrame.rgba_data.Length)
                    image[x, y] = new Rgba32(
                        gifFrame.rgba_data[offset],
                        gifFrame.rgba_data[offset + 1],
                        gifFrame.rgba_data[offset + 2],
                        gifFrame.rgba_data[offset + 3]);
            }

            var delayMs = gifFrame.delay_centiseconds * 10;
            frames[i] = new AnimationFrame<Rgba32>(image, delayMs);
        }

        return frames;
    }

    /// <summary>
    ///     将 RGBA32 图像转换为 PNG 图像数据。
    /// </summary>
    /// <param name="image">RGBA32 图像。</param>
    /// <returns>PNG 图像数据。</returns>
    public static PngImageData from_rgba32_to_png(Image<Rgba32> image)
    {
        var rawPixelData = new byte[image.height * (image.width * 4 + 1)];

        for (var y = 0; y < image.height; y++)
        {
            var rowOffset = y * (image.width * 4 + 1);
            rawPixelData[rowOffset] = (byte)PngFilterType.none;

            for (var x = 0; x < image.width; x++)
            {
                var pixel = image[x, y];
                var pixelOffset = rowOffset + 1 + x * 4;
                rawPixelData[pixelOffset] = pixel.r;
                rawPixelData[pixelOffset + 1] = pixel.g;
                rawPixelData[pixelOffset + 2] = pixel.b;
                rawPixelData[pixelOffset + 3] = pixel.a;
            }
        }

        return new PngImageData
        {
            width = image.width,
            height = image.height,
            bit_depth = 8,
            color_type = PngColorType.truecolor_alpha,
            compression_method = PngCompressionMethod.deflate,
            filter_method = PngFilterMethod.adaptive,
            interlace_method = PngInterlaceMethod.none,
            raw_pixel_data = rawPixelData
        };
    }

    /// <summary>
    ///     将 RGBA32 图像转换为 BMP 图像数据。
    /// </summary>
    /// <param name="image">RGBA32 图像。</param>
    /// <returns>BMP 图像数据。</returns>
    public static BmpImageData from_rgba32_to_bmp(Image<Rgba32> image)
    {
        var stride = (image.width * 32 + 31) / 32 * 4;
        var pixelData = new byte[stride * image.height];

        for (var y = 0; y < image.height; y++)
        {
            var dstY = image.height - 1 - y;
            var rowOffset = dstY * stride;

            for (var x = 0; x < image.width; x++)
            {
                var pixel = image[x, y];
                var offset = rowOffset + x * 4;
                pixelData[offset] = pixel.b;
                pixelData[offset + 1] = pixel.g;
                pixelData[offset + 2] = pixel.r;
                pixelData[offset + 3] = pixel.a;
            }
        }

        return new BmpImageData
        {
            width = image.width,
            height = image.height,
            bits_per_pixel = 32,
            compression = BmpCompression.none,
            image_size = (uint)(stride * image.height),
            x_pels_per_meter = 3780,
            y_pels_per_meter = 3780,
            pixel_data = pixelData
        };
    }

    private static List<GifFrame> DecodeGifToRgba(GifImageData gif)
    {
        var frames = new List<GifFrame>();
        var canvas = new byte[gif.width * gif.height * 4];
        var prevCanvas = new byte[gif.width * gif.height * 4];

        foreach (var frame in gif.frames)
        {
            var palette = frame.local_color_table.Length > 0
                ? frame.local_color_table
                : gif.global_color_table;

            if (frame.disposal_method == GifDisposalMethod.restore_to_previous)
                Array.Copy(canvas, prevCanvas, canvas.Length);

            CompositeGifFrame(canvas, frame, palette, gif);

            var rgbaData = new byte[gif.width * gif.height * 4];
            Array.Copy(canvas, rgbaData, canvas.Length);

            frames.Add(new GifFrame
            {
                width = gif.width,
                height = gif.height,
                delay_centiseconds = frame.delay_centiseconds,
                disposal_method = frame.disposal_method,
                rgba_data = rgbaData
            });

            switch (frame.disposal_method)
            {
                case GifDisposalMethod.restore_to_background:
                    ClearGifRegion(canvas, frame.left, frame.top, frame.width, frame.height, gif.width);
                    break;
                case GifDisposalMethod.restore_to_previous:
                    Array.Copy(prevCanvas, canvas, canvas.Length);
                    break;
            }
        }

        return frames;
    }

    private static void CompositeGifFrame(byte[] canvas, GifImageFrame frame, byte[] palette, GifImageData gifData)
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

    private static void ClearGifRegion(byte[] canvas, int left, int top, int width, int height, int canvasWidth)
    {
        for (var y = top; y < top + height; y++)
        for (var x = left; x < left + width; x++)
        {
            var idx = (y * canvasWidth + x) * 4;
            if (idx + 3 < canvas.Length)
            {
                canvas[idx] = 0;
                canvas[idx + 1] = 0;
                canvas[idx + 2] = 0;
                canvas[idx + 3] = 0;
            }
        }
    }
}