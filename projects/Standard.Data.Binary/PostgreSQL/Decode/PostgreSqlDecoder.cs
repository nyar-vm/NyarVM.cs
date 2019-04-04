using Std.Data.Binary.Frame;
using Std.Data.Binary.PostgreSQL.Data;

namespace Std.Data.Binary.PostgreSQL.Decode;

/// <summary>
///     PostgreSQL 解码器，用于解析 PostgreSQL 协议消息的
/// </summary>
public ref struct PostgreSqlDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="PostgreSqlDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要解码的字节数据的/param>
    public PostgreSqlDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 PostgreSQL 消息的
    /// </summary>
    /// <returns>解码后的 PostgreSQL 消息数据的/returns>
    public PostgreSqlMessageData decode_message()
    {
        var messageTypeByte = _buffer.read_u8();
        var messageType = (PostgreSqlConstants.MessageType)messageTypeByte;

        var length = _buffer.read_i32_be();

        var contentLength = length - 4;

        var data = _buffer.read_bytes(contentLength).ToArray();

        var message = new PostgreSqlMessageData
        {
            type = messageType,
            length = length,
            data = data
        };

        switch (messageType)
        {
            case PostgreSqlConstants.MessageType.authentication_request:
                decode_authentication_request(message);
                break;
            case PostgreSqlConstants.MessageType.error_response:
                decode_error_response(message);
                break;
            case PostgreSqlConstants.MessageType.command_complete:
                decode_command_complete(message);
                break;
            case PostgreSqlConstants.MessageType.ready_for_query:
                decode_ready_for_query(message);
                break;
            case PostgreSqlConstants.MessageType.row_description:
                decode_row_description(message);
                break;
        }

        return message;
    }

    /// <summary>
    ///     解码认证请求消息的
    /// </summary>
    /// <param name="message">消息数据的/param>
    private void decode_authentication_request(PostgreSqlMessageData message)
    {
        var buffer = new ByteBuffer(message.data);

        var authType = (PostgreSqlConstants.AuthenticationType)buffer.read_i32_be();
        message.authentication_type = authType;

        if (buffer.remaining > 0) message.authentication_data = [.. buffer.read_bytes(buffer.remaining)];
    }

    /// <summary>
    ///     解码错误响应消息的
    /// </summary>
    /// <param name="message">消息数据的/param>
    private void decode_error_response(PostgreSqlMessageData message)
    {
        var buffer = new ByteBuffer(message.data);

        while (!buffer.is_end)
        {
            var fieldType = buffer.read_u8();
            if (fieldType == 0) break;

            var fieldValue = buffer.read_null_terminated_string();
            message.error_fields.Add(((char)fieldType).ToString(), fieldValue);
        }
    }

    /// <summary>
    ///     解码命令完成消息的
    /// </summary>
    /// <param name="message">消息数据的/param>
    private void decode_command_complete(PostgreSqlMessageData message)
    {
        var buffer = new ByteBuffer(message.data);

        message.command_tag = buffer.read_null_terminated_string();
    }

    /// <summary>
    ///     解码就绪消息的
    /// </summary>
    /// <param name="message">消息数据的/param>
    private void decode_ready_for_query(PostgreSqlMessageData message)
    {
        var buffer = new ByteBuffer(message.data);

        var transactionStatusByte = buffer.read_u8();
        message.transaction_status = (PostgreSqlConstants.TransactionStatus)transactionStatusByte;
    }

    /// <summary>
    ///     解码行描述消息的
    /// </summary>
    /// <param name="message">消息数据的/param>
    private void decode_row_description(PostgreSqlMessageData message)
    {
        var buffer = new ByteBuffer(message.data);

        var fieldCount = buffer.read_i16_be();

        for (var i = 0; i < fieldCount; i++)
        {
            var fieldDescription = new PostgreSqlFieldDescription
            {
                name = buffer.read_null_terminated_string(),
                table_id = buffer.read_u32_be(),
                column_id = buffer.read_u16_be(),
                data_type_size = buffer.read_u16_be(),
                data_type_oid = buffer.read_u32_be(),
                type_modifier = buffer.read_i32_be(),
                format_code = buffer.read_u16_be()
            };

            message.field_descriptions.Add(fieldDescription);
        }
    }
}