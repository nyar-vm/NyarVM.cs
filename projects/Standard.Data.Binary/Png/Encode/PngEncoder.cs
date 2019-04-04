using System.IO.Compression;
using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Hashing;
using Std.Data.Binary.Png.Data;

namespace Std.Data.Binary.Png.Encode;

/// <summary>
///     PNG 文件编码器，的C# 数据结构编码的PNG 图像格式的
/// </summary>
/// <remarks>
///     PNG 是无损压缩的位图格式，使的zlib/deflate 压缩，支持灰度、索引色、真彩色的Alpha 通道的
///     编码器生成符的PNG 规范的二进制数据的
/// </remarks>
public sealed class PngEncoder
{
    /// <summary>
    ///     的PNG 图像数据编码的PNG 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     PNG 图像数据的/param>
    ///     <returns>PNG 二进制数据的/returns>
    public byte[] encode(PngImageData data)
    {
        var size = estimate_size(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        write_signature(ref writer);
        write_ihdr_chunk(ref writer, data);

        if (data.palette.Length > 0) write_plte_chunk(ref writer, data.palette);

        if (data.transparency.Length > 0) write_chunk(ref writer, PngConstants.trns_tag, data.transparency);

        write_idat_chunks(ref writer, data);

        foreach (var chunk in data.ancillary_chunks) write_chunk(ref writer, chunk.type, chunk.data);

        write_iend_chunk(ref writer);

        return buffer[..writer.position];
    }

    #region 私有编码方法

    private static void write_signature(ref ByteBufferWriter writer)
    {
        writer.write(PngConstants.signature);
    }

    private static void write_ihdr_chunk(ref ByteBufferWriter writer, PngImageData data)
    {
        var ihdrData = new byte[PngConstants.ihdr_data_length];
        var ihdrWriter = new ByteBufferWriter(ihdrData);

        ihdrWriter.write_u32_be((uint)data.width);
        ihdrWriter.write_u32_be((uint)data.height);
        ihdrWriter.write_u8(data.bit_depth);
        ihdrWriter.write_u8((byte)data.color_type);
        ihdrWriter.write_u8((byte)data.compression_method);
        ihdrWriter.write_u8((byte)data.filter_method);
        ihdrWriter.write_u8((byte)data.interlace_method);

        write_chunk(ref writer, PngConstants.ihdr_tag, ihdrData[..ihdrWriter.position]);
    }

    private static void write_plte_chunk(ref ByteBufferWriter writer, byte[] palette)
    {
        write_chunk(ref writer, PngConstants.plte_tag, palette);
    }

    private static void write_idat_chunks(ref ByteBufferWriter writer, PngImageData data)
    {
        byte[] compressedData;

        if (data.raw_pixel_data.Length > 0)
        {
            compressedData = compress_data(data.raw_pixel_data);
        }
        else
        {
            var rawRowSize = data.width * data.bytes_per_pixel;
            var stride = rawRowSize + 1;
            var totalSize = stride * data.height;
            var rawData = new byte[totalSize];

            for (var y = 0; y < data.height; y++) rawData[y * stride] = (byte)PngFilterType.none;

            compressedData = compress_data(rawData);
        }

        write_chunk(ref writer, PngConstants.idat_tag, compressedData);
    }

    private static void write_iend_chunk(ref ByteBufferWriter writer)
    {
        write_chunk(ref writer, PngConstants.iend_tag, []);
    }

    private static void write_chunk(ref ByteBufferWriter writer, string type, ReadOnlySpan<byte> data)
    {
        writer.write_u32_be((uint)data.Length);
        writer.write_string(type);
        writer.write(data);
        var crc = compute_crc(type, data);
        writer.write_u32_be(crc);
    }

    private static byte[] compress_data(byte[] data)
    {
        using var outputStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal, true))
        {
            deflateStream.Write(data, 0, data.Length);
        }

        return outputStream.ToArray();
    }

    private static uint compute_crc(string type, ReadOnlySpan<byte> data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        var crc = new Crc32();

        crc.update(typeBytes);
        crc.update(data);

        return crc.value;
    }

    private static int estimate_size(PngImageData data)
    {
        var rawRowSize = data.width * data.bytes_per_pixel + 1;
        var rawSize = rawRowSize * data.height;
        return PngConstants.signature_length + rawSize + 4096 + data.palette.Length +
               data.ancillary_chunks.Sum(c => c.data.Length + 12);
    }

    #endregion
}