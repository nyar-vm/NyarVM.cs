using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Redis.Data;

namespace Std.Data.Binary.Redis.Encode;

/// <summary>
///     Redis 编码器，用于编码 Redis RESP 协议消息的
/// </summary>
public ref struct RedisEncoder
{
    private ByteBufferWriter _writer;

    /// <summary>
    ///     初始的<see cref="RedisEncoder" /> 结构的新实例的
    /// </summary>
    /// <param name="buffer">目标字节缓冲区的/param>
    public RedisEncoder(Span<byte> buffer)
    {
        _writer = new ByteBufferWriter(buffer);
    }

    /// <summary>
    ///     编码 Redis 消息的
    /// </summary>
    /// <param name="message">要编码的消息的/param>
    public void encode_message(RedisMessageData message)
    {
        switch (message.type)
        {
            case RedisMessageType.simple_string:
                encode_simple_string(message.simple_string);
                break;
            case RedisMessageType.error:
                encode_error(message.error);
                break;
            case RedisMessageType.integer:
                encode_integer(message.integer);
                break;
            case RedisMessageType.bulk_string:
                encode_bulk_string(message.bulk_string, message.is_null_bulk_string);
                break;
            case RedisMessageType.array:
                encode_array(message.array, message.is_null_array);
                break;
        }
    }

    /// <summary>
    ///     编码简单字符串的
    /// </summary>
    /// <param name="value">要编码的字符串的/param>
    public void encode_simple_string(string value)
    {
        _writer.write_u8((byte)RedisConstants.simple_string_prefix);
        _writer.write_string(value);
        write_crlf();
    }

    /// <summary>
    ///     编码错误的
    /// </summary>
    /// <param name="error">错误消息的/param>
    public void encode_error(string error)
    {
        _writer.write_u8((byte)RedisConstants.error_prefix);
        _writer.write_string(error);
        write_crlf();
    }

    /// <summary>
    ///     编码整数的
    /// </summary>
    /// <param name="value">整数值的/param>
    public void encode_integer(long value)
    {
        _writer.write_u8((byte)RedisConstants.integer_prefix);
        _writer.write_string(value.ToString());
        write_crlf();
    }

    /// <summary>
    ///     编码批量字符串的
    /// </summary>
    /// <param name="value">
    ///     要编码的字节数组的/param>
    ///     <param name="isNull">是否为空批量字符串的/param>
    public void encode_bulk_string(byte[] value, bool isNull = false)
    {
        _writer.write_u8((byte)RedisConstants.bulk_string_prefix);

        if (isNull)
        {
            _writer.write_string(RedisConstants.null_bulk_string.ToString());
            write_crlf();
            return;
        }

        var length = value?.Length ?? 0;
        _writer.write_string(length.ToString());
        write_crlf();

        if (length > 0)
        {
            _writer.write(value);
            write_crlf();
        }
    }

    /// <summary>
    ///     编码数组的
    /// </summary>
    /// <param name="elements">
    ///     数组元素的/param>
    ///     <param name="isNull">是否为空数组的/param>
    public void encode_array(List<RedisMessageData> elements, bool isNull = false)
    {
        _writer.write_u8((byte)RedisConstants.array_prefix);

        if (isNull)
        {
            _writer.write_string(RedisConstants.null_array.ToString());
            write_crlf();
            return;
        }

        var length = elements?.Count ?? 0;
        _writer.write_string(length.ToString());
        write_crlf();

        if (length > 0)
            foreach (var element in elements!)
                encode_message(element);
    }

    /// <summary>
    ///     编码命令的
    /// </summary>
    /// <param name="command">
    ///     命令名称的/param>
    ///     <param name="args">命令参数的/param>
    public void encode_command(string command, params string[] args)
    {
        var elements = new List<RedisMessageData>
        {
            new()
            {
                type = RedisMessageType.bulk_string,
                bulk_string = Encoding.UTF8.GetBytes(command.ToUpper())
            }
        };

        foreach (var arg in args)
        {
            var item = new RedisMessageData
            {
                type = RedisMessageType.bulk_string,
                bulk_string = Encoding.UTF8.GetBytes(arg)
            };
            elements.Add(item);
        }

        encode_array(elements);
    }

    /// <summary>
    ///     写入 CRLF 行结束符的
    /// </summary>
    private void write_crlf()
    {
        _writer.write_u8((byte)'\r');
        _writer.write_u8((byte)'\n');
    }
}