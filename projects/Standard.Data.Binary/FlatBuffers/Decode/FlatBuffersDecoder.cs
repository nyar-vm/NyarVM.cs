using System.Buffers.Binary;
using System.Text;
using Std.Data.Binary.FlatBuffers.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.FlatBuffers.Decode;

/// <summary>
///     FlatBuffers 解码器的
/// </summary>
public ref struct FlatBuffersDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="FlatBuffersDecoder" /> 结构的新实例的
    /// </summary>
    public FlatBuffersDecoder(ReadOnlySpan<byte> data)
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
    ///     解码根表偏移的
    /// </summary>
    public uint decode_root_offset()
    {
        return _buffer.read_u32_le();
    }

    /// <summary>
    ///     解码指定偏移处的表的
    /// </summary>
    public FlatBufferTable decode_table(uint offset)
    {
        _buffer.position = (int)offset;

        var vtableOffset = (int)(_buffer.read_i32_le() + offset);

        var vTableSize =
            BinaryPrimitives.ReadUInt16LittleEndian(
                _buffer.data.Slice(vtableOffset + FlatBuffersConstants.v_table_size_offset));
        var objectSize =
            BinaryPrimitives.ReadUInt16LittleEndian(
                _buffer.data.Slice(vtableOffset + FlatBuffersConstants.v_table_data_offset));

        var fieldCount = (vTableSize - 4) / 2;
        var fields = new List<FlatBufferField>();

        for (var i = 0; i < fieldCount; i++)
        {
            var fieldVTableOffset =
                BinaryPrimitives.ReadUInt16LittleEndian(_buffer.data.Slice(vtableOffset + 4 + i * 2));

            if (fieldVTableOffset == 0) continue;

            fields.Add(new FlatBufferField
            {
                index = i,
                v_table_offset = fieldVTableOffset
            });
        }

        return new FlatBufferTable
        {
            table_offset = offset,
            v_table_offset = (uint)vtableOffset,
            v_table_size = vTableSize,
            fields = fields
        };
    }

    /// <summary>
    ///     读取文件标识符的
    /// </summary>
    public string read_file_identifier()
    {
        if (_buffer.length < 8) return string.Empty;

        return Encoding.ASCII.GetString(_buffer.data.Slice(4, 4));
    }
}