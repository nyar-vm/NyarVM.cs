namespace Std.Data.Binary.LZ4;

/// <summary>
///     LZ4 压缩编解码器——纯 C# 实现，无第三方依赖的
///     同时支持 LZ4 Block Format 的LZ4 Frame Format，兼的lz4 命令行工具的
/// </summary>
/// <remarks>
///     LZ4 是由 Yann Collet 设计的高性能无损压缩算法的
///     专注于极快的解压速度（约 4GB/s），适用于游戏资产管线的
///     本实现为纯托的C# 代码，可在所的.NET 平台运行（包的WASM）的
///     Compress 输出 Frame Format（兼容标准工具），Decompress 自动检测格式的
/// </remarks>
public sealed class Lz4Codec
{
    #region 常量

    private const int _min_match = 4;
    private const int _max_match_length = 0xFFFF - _min_match;
    private const int _max_offset = 0xFFFF;
    private const int _hash_log = 16;
    private const int _hash_size = 1 << _hash_log;
    private const int _skip_trigger = 6;
    private const int _block_size = 0x10000;
    private const uint _frame_magic = 0x184D2204;
    private const int _block_max_size_id = 4;

    #endregion

    #region 公开方法

    /// <summary>
    ///     压缩字节数据的LZ4 Frame Format
    /// </summary>
    /// <param name="source">原始数据。</param>
    /// <returns>压缩后的数据（LZ4 Frame Format的/returns>
    public static byte[] compress(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0) return build_empty_frame();

        var maxOutput = source.Length + source.Length / 255 + 16 + 19 + source.Length / _block_size * 8;
        var output = new byte[maxOutput];
        var outputPos = 0;

        write_u32_le(output, ref outputPos, _frame_magic);

        var flg = 0x40 | 0x20 | 0x08;
        output[outputPos++] = (byte)flg;
        output[outputPos++] = _block_max_size_id << 4;

        write_u64_le(output, ref outputPos, (ulong)source.Length);

        output[outputPos++] = compute_header_checksum(output, 4, outputPos - 4 - 1);

        var srcPos = 0;
        while (srcPos < source.Length)
        {
            var blockSize = System.Math.Min(_block_size, source.Length - srcPos);
            var blockData = source.Slice(srcPos, blockSize);
            var compressed = compress_block(blockData);

            if (compressed.Length < blockSize)
            {
                write_u32_le(output, ref outputPos, (uint)compressed.Length);
                ensure_capacity(ref output, outputPos, compressed.Length);
                compressed.CopyTo(output.AsSpan(outputPos));
                outputPos += compressed.Length;
            }
            else
            {
                write_u32_le(output, ref outputPos, (uint)blockSize | 0x80000000);
                ensure_capacity(ref output, outputPos, blockSize);
                blockData.CopyTo(output.AsSpan(outputPos));
                outputPos += blockSize;
            }

            srcPos += blockSize;
        }

        write_u32_le(output, ref outputPos, 0);

        return output[..outputPos];
    }

    /// <summary>
    ///     解压 LZ4 数据，自动检的Frame Format 的Block Format
    /// </summary>
    /// <param name="source">压缩数据。</param>
    /// <param name="maxDecompressedSize">
    ///     预期解压后最大大小（的Block Format 使用的/param>
    ///     <returns>解压后的数据。</returns>
    public static byte[] decompress(ReadOnlySpan<byte> source, int maxDecompressedSize = -1)
    {
        if (source.Length == 0) return [];

        if (source.Length >= 4)
        {
            var magic = (uint)(source[0] | (source[1] << 8) | (source[2] << 16) | (source[3] << 24));
            if (magic == _frame_magic) return decompress_frame(source);
        }

        return decompress_block(source, maxDecompressedSize);
    }

    /// <summary>
    ///     压缩字节数据的LZ4 Block Format（原始块格式的
    /// </summary>
    /// <param name="source">原始数据。</param>
    /// <returns>压缩后的数据（Block Format的/returns>
    public static byte[] compress_block(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0) return [];

        var maxOutput = source.Length - source.Length / 255 + 16;
        var output = new byte[maxOutput];
        var outputPos = 0;

        if (source.Length < _min_match + 1)
        {
            output[outputPos++] = (byte)source.Length;
            source.CopyTo(output.AsSpan(outputPos));
            outputPos += source.Length;
            return output[..outputPos];
        }

        var hashTable = new int[_hash_size];
        Array.Fill(hashTable, -1);

        var srcPos = 0;
        var anchor = 0;

        while (srcPos < source.Length - _min_match)
        {
            var hash = hash4(source, srcPos);
            var refPos = hashTable[hash];
            hashTable[hash] = srcPos;

            if (refPos < 0 || srcPos - refPos > _max_offset || !equal4(source, refPos, srcPos))
            {
                srcPos += (srcPos - anchor) >> (_skip_trigger + 1);
                continue;
            }

            var literalLength = srcPos - anchor;
            var matchLength = count_match(source, refPos + _min_match, srcPos + _min_match);

            outputPos = encode_token(output, outputPos, literalLength, matchLength - _min_match);
            source.Slice(anchor, literalLength).CopyTo(output.AsSpan(outputPos));
            outputPos += literalLength;

            var offset = (ushort)(srcPos - refPos);
            output[outputPos++] = (byte)(offset & 0xFF);
            output[outputPos++] = (byte)(offset >> 8);

            anchor = srcPos + matchLength;
            srcPos = anchor;

            if (srcPos < source.Length - _min_match)
            {
                hashTable[hash4(source, srcPos - 2)] = srcPos - 2;
                hashTable[hash4(source, srcPos - 1)] = srcPos - 1;
            }
        }

        var remaining = source.Length - anchor;
        outputPos = encode_token(output, outputPos, remaining, 0);
        source.Slice(anchor, remaining).CopyTo(output.AsSpan(outputPos));
        outputPos += remaining;

        return output[..outputPos];
    }

    /// <summary>
    ///     解压 LZ4 Block Format 数据
    /// </summary>
    /// <param name="source">
    ///     压缩数据（Block Format的/param>
    ///     <param name="maxDecompressedSize">
    ///         预期解压后最大大的/param>
    ///         <returns>解压后的数据。</returns>
    public static byte[] decompress_block(ReadOnlySpan<byte> source, int maxDecompressedSize = -1)
    {
        if (source.Length == 0) return [];

        if (maxDecompressedSize < 0) maxDecompressedSize = source.Length * 4 + _block_size;

        var output = new byte[maxDecompressedSize];
        var outputPos = 0;
        var srcPos = 0;

        while (srcPos < source.Length)
        {
            var token = source[srcPos++];
            var literalLength = (token >> 4) & 0x0F;
            var matchLength = token & 0x0F;

            if (literalLength == 15)
                while (srcPos < source.Length)
                {
                    var extra = source[srcPos++];
                    literalLength += extra;
                    if (extra != 255) break;
                }

            if (srcPos + literalLength > source.Length) throw new InvalidDataException("LZ4 解压错误：字面量超出输入范围");

            ensure_capacity(ref output, outputPos, literalLength);
            source.Slice(srcPos, literalLength).CopyTo(output.AsSpan(outputPos));
            srcPos += literalLength;
            outputPos += literalLength;

            if (srcPos >= source.Length) break;

            if (srcPos + 2 > source.Length) throw new InvalidDataException("LZ4 解压错误：偏移量超出输入范围");

            var offset = source[srcPos] | (source[srcPos + 1] << 8);
            srcPos += 2;

            if (offset == 0) throw new InvalidDataException("LZ4 解压错误：偏移量为零");

            if (matchLength == 15)
                while (srcPos < source.Length)
                {
                    var extra = source[srcPos++];
                    matchLength += extra;
                    if (extra != 255) break;
                }

            matchLength += _min_match;

            var matchSrc = outputPos - offset;
            if (matchSrc < 0) throw new InvalidDataException("LZ4 解压错误：匹配偏移超出输出范围。");

            ensure_capacity(ref output, outputPos, matchLength);

            if (offset >= matchLength)
                Array.Copy(output, matchSrc, output, outputPos, matchLength);
            else
                for (var i = 0; i < matchLength; i++)
                    output[outputPos + i] = output[matchSrc + i];

            outputPos += matchLength;
        }

        return output[..outputPos];
    }

    #endregion

    #region Frame Format

    private static byte[] decompress_frame(ReadOnlySpan<byte> source)
    {
        var srcPos = 0;

        var magic = read_u32_le(source, ref srcPos);
        if (magic != _frame_magic) throw new InvalidDataException($"LZ4 帧魔数无效：0x{magic:X8}");

        var flg = source[srcPos++];
        var bd = source[srcPos++];

        var version = (flg >> 6) & 0x03;
        if (version != 1) throw new InvalidDataException($"LZ4 帧版本不支持：{version}");

        var blockIndependence = (flg & 0x20) != 0;
        var hasBlockChecksum = (flg & 0x10) != 0;
        var hasContentSize = (flg & 0x08) != 0;
        var hasContentChecksum = (flg & 0x04) != 0;
        var hasDictId = (flg & 0x01) != 0;

        long contentSize = 0;
        if (hasContentSize) contentSize = (long)read_u64_le(source, ref srcPos);

        srcPos++;

        if (hasDictId) srcPos += 4;

        var maxOutput = contentSize > 0 ? (int)contentSize : source.Length * 4;
        var output = new byte[maxOutput];
        var outputPos = 0;

        while (true)
        {
            if (srcPos + 4 > source.Length) throw new InvalidDataException("LZ4 帧解压错误：块头部超出范围。");

            var blockHeader = read_u32_le(source, ref srcPos);
            if (blockHeader == 0) break;

            var isUncompressed = (blockHeader & 0x80000000) != 0;
            var blockSize = (int)(blockHeader & 0x7FFFFFFF);

            if (srcPos + blockSize > source.Length) throw new InvalidDataException("LZ4 帧解压错误：块数据超出范围。");

            if (isUncompressed)
            {
                ensure_capacity(ref output, outputPos, blockSize);
                source.Slice(srcPos, blockSize).CopyTo(output.AsSpan(outputPos));
                outputPos += blockSize;
            }
            else
            {
                var decompressed = decompress_block(source.Slice(srcPos, blockSize));
                ensure_capacity(ref output, outputPos, decompressed.Length);
                decompressed.CopyTo(output.AsSpan(outputPos));
                outputPos += decompressed.Length;
            }

            srcPos += blockSize;

            if (hasBlockChecksum) srcPos += 4;
        }

        if (hasContentChecksum) srcPos += 4;

        return output[..outputPos];
    }

    private static byte[] build_empty_frame()
    {
        var frame = new byte[11];
        var pos = 0;
        write_u32_le(frame, ref pos, _frame_magic);
        frame[pos++] = 0x40 | 0x20;
        frame[pos++] = 0x40;
        write_u64_le(frame, ref pos, 0);
        frame[pos++] = 0;
        write_u32_le(frame, ref pos, 0);
        return frame[..pos];
    }

    #endregion

    #region 私有方法

    private static uint hash4(ReadOnlySpan<byte> data, int pos)
    {
        var v = (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
        return (v * 2654435761u) >> (32 - _hash_log);
    }

    private static bool equal4(ReadOnlySpan<byte> data, int a, int b)
    {
        return data[a] == data[b] && data[a + 1] == data[b + 1] &&
               data[a + 2] == data[b + 2] && data[a + 3] == data[b + 3];
    }

    private static int count_match(ReadOnlySpan<byte> data, int a, int b)
    {
        var maxLen = System.Math.Min(data.Length - a, data.Length - b);
        var len = 0;
        while (len < maxLen && data[a + len] == data[b + len]) len++;
        return System.Math.Min(len, _max_match_length + _min_match);
    }

    private static int encode_token(byte[] output, int pos, int literalLength, int matchLength)
    {
        var token = (byte)((System.Math.Min(literalLength, 15) << 4) | System.Math.Min(matchLength, 15));
        output[pos++] = token;

        if (literalLength >= 15)
        {
            var remaining = literalLength - 15;
            while (remaining >= 255)
            {
                output[pos++] = 255;
                remaining -= 255;
            }

            output[pos++] = (byte)remaining;
        }

        if (matchLength >= 15)
        {
            var remaining = matchLength - 15;
            while (remaining >= 255)
            {
                output[pos++] = 255;
                remaining -= 255;
            }

            output[pos++] = (byte)remaining;
        }

        return pos;
    }

    private static byte compute_header_checksum(byte[] data, int start, int length)
    {
        var hash = 0x9E3779B1;
        for (var i = start; i < start + length; i++)
        {
            hash ^= data[i];
            hash *= 0x85EBCA6B;
            hash = (hash << 13) | (hash >> 19);
        }

        return (byte)((hash >> 8) & 0xFF);
    }

    private static void ensure_capacity(ref byte[] buffer, int position, int needed)
    {
        if (position + needed <= buffer.Length) return;

        var newSize = System.Math.Max(buffer.Length * 2, position + needed + _block_size);
        var newBuffer = new byte[newSize];
        Array.Copy(buffer, newBuffer, position);
        buffer = newBuffer;
    }

    private static void write_u32_le(byte[] buffer, ref int pos, uint value)
    {
        buffer[pos++] = (byte)(value & 0xFF);
        buffer[pos++] = (byte)((value >> 8) & 0xFF);
        buffer[pos++] = (byte)((value >> 16) & 0xFF);
        buffer[pos++] = (byte)((value >> 24) & 0xFF);
    }

    private static void write_u64_le(byte[] buffer, ref int pos, ulong value)
    {
        write_u32_le(buffer, ref pos, (uint)(value & 0xFFFFFFFF));
        write_u32_le(buffer, ref pos, (uint)(value >> 32));
    }

    private static uint read_u32_le(ReadOnlySpan<byte> data, ref int pos)
    {
        var value = (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
        pos += 4;
        return value;
    }

    private static ulong read_u64_le(ReadOnlySpan<byte> data, ref int pos)
    {
        var low = read_u32_le(data, ref pos);
        var high = read_u32_le(data, ref pos);
        return low | ((ulong)high << 32);
    }

    #endregion
}