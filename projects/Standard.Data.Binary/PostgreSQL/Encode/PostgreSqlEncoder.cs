using Std.Data.Binary.Frame;
using Std.Data.Binary.PostgreSQL.Data;

namespace Std.Data.Binary.PostgreSQL.Encode;

/// <summary>
///     PostgreSQL 编码器，用于编码 PostgreSQL 协议消息的
/// </summary>
public ref struct PostgreSqlEncoder
{
    private ByteBufferWriter _writer;

    /// <summary>
    ///     初始的<see cref="PostgreSqlEncoder" /> 结构的新实例的
    /// </summary>
    /// <param name="buffer">目标字节缓冲区的/param>
    public PostgreSqlEncoder(Span<byte> buffer)
    {
        _writer = new ByteBufferWriter(buffer);
    }

    /// <summary>
    ///     编码 PostgreSQL 消息的
    /// </summary>
    /// <param name="message">要编码的消息的/param>
    public void encode_message(PostgreSqlMessageData message)
    {
        _writer.write_u8((byte)message.type);

        var length = (message.data?.Length ?? 0) + 4;
        _writer.write_i32_be(length);

        if (message.data is { Length: > 0 }) _writer.write(message.data);
    }

    /// <summary>
    ///     编码启动消息的
    /// </summary>
    /// <param name="user">
    ///     用户名的/param>
    ///     <param name="database">
    ///         数据库名的/param>
    ///         <param name="applicationName">应用程序名的/param>
    public void encode_startup_message(string user, string database,
        string applicationName = "Nyar.Protocol.PostgreSQL")
    {
        Span<byte> temp = stackalloc byte[4096];
        var writer = new ByteBufferWriter(temp);

        writer.write_i32_be(PostgreSqlConstants.protocol_version);

        writer.write_null_terminated_string("user");
        writer.write_null_terminated_string(user);

        if (!string.IsNullOrEmpty(database))
        {
            writer.write_null_terminated_string("database");
            writer.write_null_terminated_string(database);
        }

        writer.write_null_terminated_string("application_name");
        writer.write_null_terminated_string(applicationName);

        writer.write_u8(0);

        var length = writer.position + 4;
        _writer.write_i32_be(length);

        _writer.write(temp.Slice(0, writer.position).ToArray());
    }

    /// <summary>
    ///     编码查询消息的
    /// </summary>
    /// <param name="query">查询语句的/param>
    public void encode_query_message(string query)
    {
        Span<byte> temp = stackalloc byte[4096];
        var writer = new ByteBufferWriter(temp);

        writer.write_null_terminated_string(query);

        var message = new PostgreSqlMessageData
        {
            type = PostgreSqlConstants.MessageType.query,
            length = writer.position + 4,
            data = [.. temp.Slice(0, writer.position)]
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码密码消息的
    /// </summary>
    /// <param name="password">密码的/param>
    public void encode_password_message(string password)
    {
        Span<byte> temp = stackalloc byte[4096];
        var writer = new ByteBufferWriter(temp);

        writer.write_null_terminated_string(password);

        var message = new PostgreSqlMessageData
        {
            type = (PostgreSqlConstants.MessageType)'p',
            length = writer.position + 4,
            data = [.. temp.Slice(0, writer.position)]
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码同步消息的
    /// </summary>
    public void encode_sync_message()
    {
        var message = new PostgreSqlMessageData
        {
            type = PostgreSqlConstants.MessageType.sync_message,
            length = 4,
            data = []
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码终止消息的
    /// </summary>
    public void encode_terminate_message()
    {
        var message = new PostgreSqlMessageData
        {
            type = PostgreSqlConstants.MessageType.terminate_message,
            length = 4,
            data = []
        };

        encode_message(message);
    }

    #region 服务端编码方的

    /// <summary>
    ///     编码 AuthenticationOk 消息（类的'R'，内容为 int32 0）的
    /// </summary>
    public void encode_authentication_ok()
    {
        Span<byte> temp = stackalloc byte[16];
        var w = new ByteBufferWriter(temp);
        w.write_i32_be(0);

        var message = new PostgreSqlMessageData
        {
            type = (PostgreSqlConstants.MessageType)'R',
            length = w.position + 4,
            data = [.. temp.Slice(0, w.position)]
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码 RowDescription 消息（类的'T'）的
    /// </summary>
    /// <param name="columns">列元数据列表的/param>
    public void encode_row_description(
        IReadOnlyList<(string Name, uint TableOid, short ColumnAttr, int TypeOid, short TypeSize, int TypeModifier,
            short FormatCode)> columns)
    {
        Span<byte> temp = stackalloc byte[8192];
        var w = new ByteBufferWriter(temp);

        w.write_i16_be((short)columns.Count);

        foreach (var col in columns)
        {
            w.write_null_terminated_string(col.Name);
            w.write_i32_be((int)col.TableOid);
            w.write_i16_be(col.ColumnAttr);
            w.write_i32_be(col.TypeOid);
            w.write_i16_be(col.TypeSize);
            w.write_i32_be(col.TypeModifier);
            w.write_i16_be(col.FormatCode);
        }

        var message = new PostgreSqlMessageData
        {
            type = (PostgreSqlConstants.MessageType)'T',
            length = w.position + 4,
            data = [.. temp.Slice(0, w.position)]
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码 DataRow 消息（类的'D'）的
    /// </summary>
    /// <param name="values">行中各列的字节值，null 表示 NULL的/param>
    public void encode_data_row(IReadOnlyList<byte[]?> values)
    {
        Span<byte> temp = stackalloc byte[8192];
        var w = new ByteBufferWriter(temp);

        w.write_i16_be((short)values.Count);

        foreach (var value in values)
            if (value is null)
            {
                w.write_i32_be(-1);
            }
            else
            {
                w.write_i32_be(value.Length);
                w.write(value);
            }

        var message = new PostgreSqlMessageData
        {
            type = (PostgreSqlConstants.MessageType)'D',
            length = w.position + 4,
            data = [.. temp.Slice(0, w.position)]
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码 CommandComplete 消息（类的'C'）的
    /// </summary>
    /// <param name="tag">命令标签（如 "SELECT 42"的INSERT 0 1"）的/param>
    public void encode_command_complete(string tag)
    {
        Span<byte> temp = stackalloc byte[256];
        var w = new ByteBufferWriter(temp);

        w.write_null_terminated_string(tag);

        var message = new PostgreSqlMessageData
        {
            type = (PostgreSqlConstants.MessageType)'C',
            length = w.position + 4,
            data = [.. temp.Slice(0, w.position)]
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码 ReadyForQuery 消息（类的'Z'）的
    /// </summary>
    /// <param name="transactionStatus">事务状态（'I'=空闲, 'T'=事务的 'E'=失败事务）的/param>
    public void encode_ready_for_query(char transactionStatus = 'I')
    {
        Span<byte> temp = stackalloc byte[16];
        var w = new ByteBufferWriter(temp);

        w.write_u8((byte)transactionStatus);

        var message = new PostgreSqlMessageData
        {
            type = (PostgreSqlConstants.MessageType)'Z',
            length = w.position + 4,
            data = [.. temp.Slice(0, w.position)]
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码 ErrorResponse 消息（类的'E'）的
    /// </summary>
    /// <param name="severity">
    ///     严重程度（如 "ERROR"）的/param>
    ///     <param name="sqlState">
    ///         5 字符 SQL 状态码（如 "42601"）的/param>
    ///         <param name="message">错误消息文本的/param>
    public void encode_error_response(string severity, string sqlState, string message)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        w.write_u8((byte)'S');
        w.write_null_terminated_string(severity);

        w.write_u8((byte)'C');
        w.write_null_terminated_string(sqlState);

        w.write_u8((byte)'M');
        w.write_null_terminated_string(message);

        w.write_u8(0);

        var msg = new PostgreSqlMessageData
        {
            type = (PostgreSqlConstants.MessageType)'E',
            length = w.position + 4,
            data = [.. temp.Slice(0, w.position)]
        };

        encode_message(msg);
    }

    /// <summary>
    ///     编码 ParseComplete 消息（类的'1'）的
    /// </summary>
    public void encode_parse_complete()
    {
        var message = new PostgreSqlMessageData
        {
            type = PostgreSqlConstants.MessageType.parse_complete,
            length = 4,
            data = []
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码 BindComplete 消息（类的'2'）的
    /// </summary>
    public void encode_bind_complete()
    {
        var message = new PostgreSqlMessageData
        {
            type = PostgreSqlConstants.MessageType.bind_complete,
            length = 4,
            data = []
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码 CloseComplete 消息（类的'3'）的
    /// </summary>
    public void encode_close_complete()
    {
        var message = new PostgreSqlMessageData
        {
            type = PostgreSqlConstants.MessageType.close_complete,
            length = 4,
            data = []
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码 NoData 消息（类的'n'）的
    /// </summary>
    public void encode_no_data()
    {
        var message = new PostgreSqlMessageData
        {
            type = (PostgreSqlConstants.MessageType)'n',
            length = 4,
            data = []
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码 ParameterDescription 消息（类的't'）的
    ///     包含参数类型 OID 列表的
    /// </summary>
    /// <param name="parameterOids">参数类型 OID 列表，可为空。</param>
    public void encode_parameter_description(IReadOnlyList<int>? parameterOids)
    {
        Span<byte> temp = stackalloc byte[256];
        var w = new ByteBufferWriter(temp);

        if (parameterOids is null || parameterOids.Count == 0)
        {
            w.write_i16_be(0);
        }
        else
        {
            w.write_i16_be((short)parameterOids.Count);
            foreach (var oid in parameterOids) w.write_i32_be(oid);
        }

        var message = new PostgreSqlMessageData
        {
            type = PostgreSqlConstants.MessageType.parameter_description,
            length = w.position + 4,
            data = [.. temp.Slice(0, w.position)]
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码 EmptyQueryResponse 消息（类的'I'）的
    /// </summary>
    public void encode_empty_query_response()
    {
        var message = new PostgreSqlMessageData
        {
            type = PostgreSqlConstants.MessageType.empty_query_response,
            length = 4,
            data = []
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码 PortalSuspended 消息（类的's'）的
    /// </summary>
    public void encode_portal_suspended()
    {
        var message = new PostgreSqlMessageData
        {
            type = PostgreSqlConstants.MessageType.portal_suspended,
            length = 4,
            data = []
        };

        encode_message(message);
    }

    #endregion
}