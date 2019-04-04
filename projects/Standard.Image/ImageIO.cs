using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core.Media;
using Core.Media.Image;
using Std.Data.Binary.Bmp.Decode;
using Std.Data.Binary.Bmp.Encode;
using Std.Data.Binary.Gif.Data;
using Std.Data.Binary.Gif.Decode;
using Std.Data.Binary.Gif.Encode;
using Std.Data.Binary.Png.Data;
using Std.Data.Binary.Png.Decode;
using Std.Data.Binary.Png.Encode;
using Std.Image.Formats;
using Std.Image.Interop;
using Std.Image.Pixels;
using Std.Media;

namespace Std.Image;

/// <summary>
///     图像输入输出静态类，提供统一的图像加载和保存门面。
/// </summary>
public static class ImageIO
{
    private static readonly ImageFormatRegistry s_registry = CreateDefaultRegistry();

    /// <summary>
    ///     从二进制数据加载 RGBA32 图像。
    /// </summary>
    /// <param name="data">图像二进制数据。</param>
    /// <returns>RGBA32 图像。</returns>
    public static Image<Rgba32> load(ReadOnlySpan<byte> data)
    {
        var formatName = s_registry.detect_format_name(data);

        if (formatName is null) throw new InvalidDataException("无法识别图像格式。");

        var handler = s_registry.get_handler(formatName);

        if (handler is null) throw new InvalidDataException($"未找到格式 \"{formatName}\" 的处理器。");

        var image = handler.load(data);

        if (image is Image<Rgba32> rgbaImage) return rgbaImage;

        throw new InvalidDataException("加载的图像不是 RGBA32 格式。");
    }

    /// <summary>
    ///     从文件路径加载 RGBA32 图像。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <returns>RGBA32 图像。</returns>
    public static Image<Rgba32> load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return load(bytes);
    }

    /// <summary>
    ///     从二进制数据加载动画帧数组。
    /// </summary>
    /// <param name="data">图像二进制数据。</param>
    /// <returns>动画帧数组。</returns>
    public static AnimationFrame<Rgba32>[] load_animated(ReadOnlySpan<byte> data)
    {
        var formatName = s_registry.detect_format_name(data);

        if (formatName is null) throw new InvalidDataException("无法识别图像格式。");

        return formatName.ToUpperInvariant() switch
        {
            "GIF" => LoadGifFrames(data),
            _ => throw new InvalidDataException($"格式 \"{formatName}\" 不支持动画加载。")
        };
    }

    /// <summary>
    ///     从文件路径加载动画帧数组。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <returns>动画帧数组。</returns>
    public static AnimationFrame<Rgba32>[] load_animated(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return load_animated(bytes);
    }

    /// <summary>
    ///     将 RGBA32 图像保存为 PNG 格式。
    /// </summary>
    /// <param name="image">RGBA32 图像。</param>
    /// <returns>PNG 二进制数据。</returns>
    public static byte[] save_png(Image<Rgba32> image)
    {
        var pngData = AcornDataMapper.from_rgba32_to_png(image);
        var encoder = new PngEncoder();
        return encoder.encode(pngData);
    }

    /// <summary>
    ///     将 RGBA32 图像保存为 BMP 格式。
    /// </summary>
    /// <param name="image">RGBA32 图像。</param>
    /// <returns>BMP 二进制数据。</returns>
    public static byte[] save_bmp(Image<Rgba32> image)
    {
        var bmpData = AcornDataMapper.from_rgba32_to_bmp(image);
        var encoder = new BmpEncoder();
        return encoder.encode(bmpData);
    }

    /// <summary>
    ///     将动画帧列表保存为 GIF 格式。
    /// </summary>
    /// <param name="frames">动画帧列表。</param>
    /// <param name="loop_count">循环次数，0 表示无限循环。</param>
    /// <returns>GIF 二进制数据。</returns>
    public static byte[] save_gif(IReadOnlyList<AnimationFrame<Rgba32>> frames, int loop_count = 0)
    {
        var gifFrames = new List<GifFrame>();

        foreach (var frame in frames)
        {
            var rgbaData = new byte[frame.image.width * frame.image.height * 4];

            for (var y = 0; y < frame.image.height; y++)
            for (var x = 0; x < frame.image.width; x++)
            {
                var pixel = frame.image[x, y];
                var offset = (y * frame.image.width + x) * 4;
                rgbaData[offset] = pixel.r;
                rgbaData[offset + 1] = pixel.g;
                rgbaData[offset + 2] = pixel.b;
                rgbaData[offset + 3] = pixel.a;
            }

            gifFrames.Add(new GifFrame
            {
                width = frame.image.width,
                height = frame.image.height,
                delay_centiseconds = frame.delay_ms / 10,
                disposal_method = GifDisposalMethod.none,
                rgba_data = rgbaData
            });
        }

        var encoder = new GifEncoder();
        return encoder.encode(gifFrames, loop_count);
    }

    private static AnimationFrame<Rgba32>[] LoadGifFrames(ReadOnlySpan<byte> data)
    {
        var decoder = new GifDecoder(data);
        var gifData = decoder.decode();
        return AcornDataMapper.to_gif_frames(gifData);
    }

    private static ImageFormatRegistry CreateDefaultRegistry()
    {
        var registry = new ImageFormatRegistry();

        registry.register_detector(new PngFormatDetector());
        registry.register_detector(new BmpFormatDetector());
        registry.register_detector(new GifFormatDetector());

        registry.register_handler(new PngFormatHandler());
        registry.register_handler(new BmpFormatHandler());

        return registry;
    }

    #region 内部辅助方法

    private static Image<Rgba32> ConvertToRgba32<TPixel>(Image<TPixel> image) where TPixel : unmanaged
    {
        if (image is Image<Rgba32> rgbaImage) return rgbaImage;

        var result = new Image<Rgba32>(image.width, image.height, PixelFormat.rgba32);

        for (var y = 0; y < image.height; y++)
        for (var x = 0; x < image.width; x++)
        {
            var pixel = image[x, y];
            var bytes = new byte[Unsafe.SizeOf<TPixel>()];
            MemoryMarshal.Write(bytes, ref pixel);

            var r = bytes.Length > 0 ? bytes[0] : (byte)0;
            var g = bytes.Length > 1 ? bytes[1] : (byte)0;
            var b = bytes.Length > 2 ? bytes[2] : (byte)0;
            var a = bytes.Length > 3 ? bytes[3] : (byte)255;

            result[x, y] = new Rgba32(r, g, b, a);
        }

        return result;
    }

    #endregion

    #region 内置格式探测器

    private sealed class PngFormatDetector : IImageFormatDetector
    {
        public string format_name => "PNG";

        public IEnumerable<string> file_extensions => ["png"];

        public bool detect(ReadOnlySpan<byte> header)
        {
            if (header.Length < PngConstants.signature_length) return false;

            var sig = PngConstants.signature;

            for (var i = 0; i < PngConstants.signature_length; i++)
                if (header[i] != sig[i])
                    return false;

            return true;
        }

        public bool detect(string extension)
        {
            return extension.Equals("png", StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class BmpFormatDetector : IImageFormatDetector
    {
        public string format_name => "BMP";

        public IEnumerable<string> file_extensions => ["bmp"];

        public bool detect(ReadOnlySpan<byte> header)
        {
            return header.Length >= 2 && header[0] == 0x42 && header[1] == 0x4D;
        }

        public bool detect(string extension)
        {
            return extension.Equals("bmp", StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class GifFormatDetector : IImageFormatDetector
    {
        public string format_name => "GIF";

        public IEnumerable<string> file_extensions => ["gif"];

        public bool detect(ReadOnlySpan<byte> header)
        {
            if (header.Length < 6) return false;

            var sig87a = GifConstants.signature87_a;
            var sig89a = GifConstants.signature89_a;

            var match87a = true;
            var match89a = true;

            for (var i = 0; i < 6; i++)
            {
                if (header[i] != sig87a[i]) match87a = false;

                if (header[i] != sig89a[i]) match89a = false;
            }

            return match87a || match89a;
        }

        public bool detect(string extension)
        {
            return extension.Equals("gif", StringComparison.OrdinalIgnoreCase);
        }
    }

    #endregion

    #region 内置格式处理器

    private sealed class PngFormatHandler : IImageFormatHandler
    {
        public string format_name => "PNG";

        public IEnumerable<string> file_extensions => ["png"];

        public IImage load(ReadOnlySpan<byte> data)
        {
            var decoder = new PngDecoder(data);
            var pngData = decoder.decode();
            return AcornDataMapper.to_rgba32(pngData);
        }

        public byte[] save<TPixel>(Image<TPixel> image) where TPixel : unmanaged
        {
            var rgbaImage = ConvertToRgba32(image);
            return save_png(rgbaImage);
        }
    }

    private sealed class BmpFormatHandler : IImageFormatHandler
    {
        public string format_name => "BMP";

        public IEnumerable<string> file_extensions => ["bmp"];

        public IImage load(ReadOnlySpan<byte> data)
        {
            var decoder = new BmpDecoder(data);
            var bmpData = decoder.decode();
            return AcornDataMapper.to_rgba32(bmpData);
        }

        public byte[] save<TPixel>(Image<TPixel> image) where TPixel : unmanaged
        {
            var rgbaImage = ConvertToRgba32(image);
            return save_bmp(rgbaImage);
        }
    }

    #endregion
}