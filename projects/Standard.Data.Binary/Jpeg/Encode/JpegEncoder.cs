using Std.Data.Binary.Jpeg.Data;

namespace Std.Data.Binary.Jpeg.Encode;

/// <summary>
///     JPEG 文件编码器，的C# 数据结构编码的JPEG 图像格式的
/// </summary>
/// <remarks>
///     JPEG 是有损压缩的位图格式，使的DCT 变换的Huffman 编码，支持灰度和 YCbCr 色彩空间的
///     编码器生成符的JPEG 规范的二进制数据的
/// </remarks>
public sealed class JpegEncoder
{
    /// <summary>
    ///     编码质量的-100），默认 85的
    /// </summary>
    public int quality { get; set; } = 85;

    /// <summary>
    ///     的JPEG 图像数据编码的JPEG 二进制格式的
    /// </summary>
    /// <param name="image">
    ///     JPEG 图像数据的/param>
    ///     <returns>JPEG 二进制数据的/returns>
    public byte[] encode(JpegImageData image)
    {
        throw new NotImplementedException("JPEG 编码尚未实现");
    }
}