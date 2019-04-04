using System.IO.Compression;

namespace Std.Database.Storage;

/// <summary>
///     基于 Brotli 的页面压缩器
/// </summary>
internal sealed class BrotliPageCompressor : IPageCompressor
{
    private readonly int _compression_level;

    public BrotliPageCompressor(int compressionLevel = 4)
    {
        _compression_level = compressionLevel;
    }

    public int max_compressed_size(int uncompressedSize)
    {
        return BrotliEncoder.GetMaxCompressedLength(uncompressedSize);
    }

    public int compress(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return BrotliEncoder.TryCompress(source, destination, out var bytesWritten, _compression_level, 22)
            ? bytesWritten
            : -1;
    }

    public int decompress(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return BrotliDecoder.TryDecompress(source, destination, out var bytesWritten)
            ? bytesWritten
            : -1;
    }
}