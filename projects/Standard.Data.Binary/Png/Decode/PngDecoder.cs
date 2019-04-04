using System.IO.Compression;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Png.Data;

namespace Std.Data.Binary.Png.Decode;

/// <summary>
///     PNG 文件解码器，的PNG 图像格式解码的C# 数据结构的
/// </summary>
/// <remarks>
///     PNG 是无损压缩的位图格式，使的zlib/deflate 压缩，支持灰度、索引色、真彩色的Alpha 通道的
///     解码器解析所有块，合的IDAT 块并解压像素数据的
/// </remarks>
public ref struct PngDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="PngDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">PNG 二进制数据的/param>
    public PngDecoder(ReadOnlySpan<byte> data)
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
    ///     解码 PNG 文件的
    /// </summary>
    /// <returns>PNG 图像数据的/returns>
    public PngImageData decode()
    {
        verify_signature();

        var width = 0;
        var height = 0;
        byte bitDepth = 0;
        PngColorType colorType = 0;
        PngCompressionMethod compressionMethod = 0;
        PngFilterMethod filterMethod = 0;
        PngInterlaceMethod interlaceMethod = 0;
        var palette = Array.Empty<byte>();
        var transparency = Array.Empty<byte>();
        var idatData = new List<byte[]>();
        var ancillaryChunks = new List<PngChunk>();

        while (!_buffer.is_end)
        {
            var chunkLength = _buffer.read_u32_be();
            var chunkType = _buffer.read_string(4);
            var chunkData = chunkLength > 0 ? _buffer.read_bytes((int)chunkLength).ToArray() : [];
            var crc = _buffer.read_u32_be();

            switch (chunkType)
            {
                case PngConstants.ihdr_tag:
                    width = (int)((uint)(chunkData[0] << 24) | (uint)(chunkData[1] << 16) | (uint)(chunkData[2] << 8) |
                                  chunkData[3]);
                    height = (int)((uint)(chunkData[4] << 24) | (uint)(chunkData[5] << 16) | (uint)(chunkData[6] << 8) |
                                   chunkData[7]);
                    bitDepth = chunkData[8];
                    colorType = (PngColorType)chunkData[9];
                    compressionMethod = (PngCompressionMethod)chunkData[10];
                    filterMethod = (PngFilterMethod)chunkData[11];
                    interlaceMethod = (PngInterlaceMethod)chunkData[12];
                    break;

                case PngConstants.plte_tag:
                    palette = chunkData;
                    break;

                case PngConstants.trns_tag:
                    transparency = chunkData;
                    break;

                case PngConstants.idat_tag:
                    idatData.Add(chunkData);
                    break;

                case PngConstants.iend_tag:
                    goto Done;

                default:
                    ancillaryChunks.Add(new PngChunk { type = chunkType, data = chunkData });
                    break;
            }
        }

        Done:
        var rawPixelData = decompress_idat(idatData);

        return new PngImageData
        {
            width = width,
            height = height,
            bit_depth = bitDepth,
            color_type = colorType,
            compression_method = compressionMethod,
            filter_method = filterMethod,
            interlace_method = interlaceMethod,
            palette = palette,
            transparency = transparency,
            raw_pixel_data = rawPixelData,
            ancillary_chunks = ancillaryChunks
        };
    }

    /// <summary>
    ///     仅解的PNG 文件头信息的
    /// </summary>
    public (int Width, int Height, byte BitDepth, PngColorType ColorType) decode_header()
    {
        verify_signature();

        var length = _buffer.read_u32_be();
        var type = _buffer.read_string(4);

        if (type != PngConstants.ihdr_tag) throw new InvalidDataException($"PNG 第一个块不是 IHDR，实际为 \"{type}\"");

        var width = (int)_buffer.read_u32_be();
        var height = (int)_buffer.read_u32_be();
        var bitDepth = _buffer.read_u8();
        var colorType = (PngColorType)_buffer.read_u8();

        return (width, height, bitDepth, colorType);
    }

    #region 私有解析方法

    private void verify_signature()
    {
        if (_buffer.remaining < PngConstants.signature_length) throw new InvalidDataException("PNG 文件数据过短");

        for (var i = 0; i < PngConstants.signature_length; i++)
        {
            var b = _buffer.read_u8();
            var expected = PngConstants.signature[i];

            if (b != expected) throw new InvalidDataException("PNG 文件签名不匹配。");
        }
    }

    private static byte[] decompress_idat(List<byte[]> idatChunks)
    {
        if (idatChunks.Count == 0) return [];

        var totalLength = 0;

        foreach (var chunk in idatChunks) totalLength += chunk.Length;

        var compressed = new byte[totalLength];
        var offset = 0;

        foreach (var chunk in idatChunks)
        {
            Buffer.BlockCopy(chunk, 0, compressed, offset, chunk.Length);
            offset += chunk.Length;
        }

        using var inputStream = new MemoryStream(compressed);
        using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        deflateStream.CopyTo(outputStream);

        return outputStream.ToArray();
    }

    #endregion
}