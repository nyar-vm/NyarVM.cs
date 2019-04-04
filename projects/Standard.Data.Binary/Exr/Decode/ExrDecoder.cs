using System.Text;
using Std.Data.Binary.Exr.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Exr.Decode;

/// <summary>
///     OpenEXR 解码器的
/// </summary>
public ref struct ExrDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="ExrDecoder" /> 结构的新实例的
    /// </summary>
    public ExrDecoder(ReadOnlySpan<byte> data)
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
    ///     解码 EXR 文件的
    /// </summary>
    public ExrImageData decode()
    {
        if (!_buffer.match_magic(ExrConstants.magic_number)) throw new InvalidDataException("EXR 魔数不匹配。");

        _buffer.consume_magic(ExrConstants.magic_number);

        var version = _buffer.read_u32_le();

        var channels = new List<ExrChannel>();
        var compression = ExrCompression.none;
        ExrBox2I displayWindow = new();
        ExrBox2I dataWindow = new();

        while (!_buffer.is_end)
        {
            var attrName = read_null_terminated_string();

            if (string.IsNullOrEmpty(attrName)) break;

            var attrType = read_null_terminated_string();
            var attrSize = (int)_buffer.read_u32_le();
            var attrEnd = _buffer.position + attrSize;

            if (attrName == "channels" && attrType == "chlist")
                channels = read_channel_list(attrSize);
            else if (attrName == "compression" && attrType == "compression")
                compression = (ExrCompression)_buffer.read_u8();
            else if (attrName == "dataWindow" && attrType == "box2i")
                dataWindow = read_box2_i();
            else if (attrName == "displayWindow" && attrType == "box2i") displayWindow = read_box2_i();

            _buffer.position = attrEnd;
        }

        return new ExrImageData
        {
            width = displayWindow.x_max - displayWindow.x_min + 1,
            height = displayWindow.y_max - displayWindow.y_min + 1,
            channels = channels,
            compression = compression,
            display_window = displayWindow,
            data_window = dataWindow
        };
    }

    private string read_null_terminated_string()
    {
        var start = _buffer.position;

        while (_position < _buffer.length && _buffer.data[_position] != 0) _position++;

        var str = Encoding.ASCII.GetString(_buffer.data[start.._position]);

        if (_position < _buffer.length) _position++;

        return str;
    }

    private int _position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    private List<ExrChannel> read_channel_list(int size)
    {
        var channels = new List<ExrChannel>();
        var end = _buffer.position + size;

        while (_buffer.position < end - 1)
        {
            var name = read_null_terminated_string();

            if (string.IsNullOrEmpty(name)) break;

            var pixelType = (ExrPixelType)_buffer.read_i32_le();
            _buffer.advance(12);

            channels.Add(new ExrChannel { name = name, pixel_type = pixelType });
        }

        return channels;
    }

    private ExrBox2I read_box2_i()
    {
        var xMin = _buffer.read_i32_le();
        var yMin = _buffer.read_i32_le();
        var xMax = _buffer.read_i32_le();
        var yMax = _buffer.read_i32_le();

        return new ExrBox2I { x_min = xMin, y_min = yMin, x_max = xMax, y_max = yMax };
    }
}