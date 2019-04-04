using Std.Data.Binary.Frame;
using Std.Data.Binary.Redis.Data;

namespace Std.Data.Binary.Redis.Decode;

/// <summary>
///     Redis 解码器，用于解析 Redis RESP 协议消息的
/// </summary>
public ref struct RedisDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="RedisDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要解码的字节数据的/param>
    public RedisDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 Redis 消息的
    /// </summary>
    /// <returns>解码后的 Redis 消息数据的/returns>
    public RedisMessageData decode_message()
    {
        var firstByte = (char)_buffer.peek(1)[0];

        switch (firstByte)
        {
            case RedisConstants.simple_string_prefix:
                return decode_simple_string();
            case RedisConstants.error_prefix:
                return decode_error();
            case RedisConstants.integer_prefix:
                return decode_integer();
            case RedisConstants.bulk_string_prefix:
                return decode_bulk_string();
            case RedisConstants.array_prefix:
                return decode_array();
            default:
                throw new FormatException($"未知的Redis 消息类型：{firstByte}");
        }
    }

    /// <summary>
    ///     解码简单字符串的
    /// </summary>
    /// <returns>解码后的简单字符串消息的/returns>
    private RedisMessageData decode_simple_string()
    {
        _buffer.read_u8();
        var line = read_line();

        var s = new RedisMessageData
        {
            type = RedisMessageType.simple_string,
            simple_string = line
        };
        return s;
    }

    /// <summary>
    ///     解码错误的
    /// </summary>
    /// <returns>解码后的错误消息的/returns>
    private RedisMessageData decode_error()
    {
        _buffer.read_u8();
        var line = read_line();

        var data = new RedisMessageData
        {
            type = RedisMessageType.error,
            error = line
        };
        return data;
    }

    /// <summary>
    ///     解码整数的
    /// </summary>
    /// <returns>解码后的整数消息的/returns>
    private RedisMessageData decode_integer()
    {
        _buffer.read_u8();
        var line = read_line();
        long.TryParse(line, out var value);

        var data = new RedisMessageData
        {
            type = RedisMessageType.integer,
            integer = value
        };
        return data;
    }

    /// <summary>
    ///     解码批量字符串的
    /// </summary>
    /// <returns>解码后的批量字符串消息的/returns>
    private RedisMessageData decode_bulk_string()
    {
        _buffer.read_u8();
        var line = read_line();

        if (!int.TryParse(line, out var length)) throw new FormatException($"无效的批量字符串长度：{line}");

        if (length == RedisConstants.null_bulk_string)
        {
            var s = new RedisMessageData
            {
                type = RedisMessageType.bulk_string,
                is_null_bulk_string = true
            };
            return s;
        }

        var data = _buffer.read_bytes(length).ToArray();
        _buffer.advance(2);

        var bulkString = new RedisMessageData
        {
            type = RedisMessageType.bulk_string,
            bulk_string = data
        };
        return bulkString;
    }

    /// <summary>
    ///     解码数组的
    /// </summary>
    /// <returns>解码后的数组消息的/returns>
    private RedisMessageData decode_array()
    {
        _buffer.read_u8();
        var line = read_line();

        if (!int.TryParse(line, out var length)) throw new FormatException($"无效的数组长度：{line}");

        if (length == RedisConstants.null_array)
        {
            var data = new RedisMessageData
            {
                type = RedisMessageType.array,
                is_null_array = true
            };
            return data;
        }

        var array = new List<RedisMessageData>();
        for (var i = 0; i < length; i++)
        {
            var element = decode_message();
            array.Add(element);
        }

        var decodeArray = new RedisMessageData
        {
            type = RedisMessageType.array,
            array = array
        };
        return decodeArray;
    }

    /// <summary>
    ///     读取一行数据（的CRLF 结尾）的
    /// </summary>
    /// <returns>读取的行内容（不的CRLF）的/returns>
    private string read_line()
    {
        var start = _buffer.position;
        var end = start;

        while (end + 1 < _buffer.length)
        {
            if (_buffer.read_u8_at(end) == '\r' && _buffer.read_u8_at(end + 1) == '\n') break;

            end++;
        }

        var length = end - start;
        var line = length > 0 ? _buffer.read_string(length) : string.Empty;
        _buffer.advance(2);

        return line;
    }
}