using Std.Data.Binary.Flac.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Flac.Decode;

/// <summary>
///     FLAC 解码器的
/// </summary>
public ref struct FlacDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="FlacDecoder" /> 结构的新实例的
    /// </summary>
    public FlacDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     当前位置的
    /// </summary>
    public int position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    /// <summary>
    ///     解码 FLAC 文件的
    /// </summary>
    public FlacAudioData decode()
    {
        if (!_buffer.match_magic(FlacConstants.stream_marker)) throw new InvalidDataException("FLAC 流标记不匹配");

        _buffer.consume_magic(FlacConstants.stream_marker);

        FlacAudioData? streamInfo = null;

        while (!_buffer.is_end)
        {
            if (_buffer.remaining < 4) break;

            var header = _buffer.read_u32_be();
            var isLast = (header & 0x80000000) != 0;
            var blockType = (FlacMetadataBlockType)((header >> 24) & 0x7F);
            var blockSize = (int)(header & 0x00FFFFFF);

            if (blockType == FlacMetadataBlockType.stream_info)
            {
                if (blockSize < 34)
                    _buffer.advance(blockSize);
                else
                    streamInfo = FlacAudioData.parse_stream_info(ref _buffer);
            }
            else
            {
                _buffer.advance(blockSize);
            }

            if (isLast) break;
        }

        return streamInfo ?? new FlacAudioData();
    }
}