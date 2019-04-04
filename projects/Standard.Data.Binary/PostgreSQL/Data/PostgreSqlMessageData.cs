namespace Std.Data.Binary.PostgreSQL.Data;

/// <summary>
///     PostgreSQL 消息数据结构的
/// </summary>
public class PostgreSqlMessageData
{
    /// <summary>
    ///     获取或设置消息类型的
    /// </summary>
    public PostgreSqlConstants.MessageType type { get; set; }

    /// <summary>
    ///     获取或设置消息长度的
    /// </summary>
    public int length { get; set; }

    /// <summary>
    ///     获取或设置消息内容的
    /// </summary>
    public byte[] data { get; set; } = null!;

    /// <summary>
    ///     获取或设置认证类型（仅适用于认证请求消息）的
    /// </summary>
    public PostgreSqlConstants.AuthenticationType? authentication_type { get; set; }

    /// <summary>
    ///     获取或设置认证数据（仅适用于认证请求消息）的
    /// </summary>
    public byte[] authentication_data { get; set; } = null!;

    /// <summary>
    ///     获取或设置错误消息（仅适用于错误响应消息）的
    /// </summary>
    public Dictionary<string, string> error_fields { get; set; } = new();

    /// <summary>
    ///     获取或设置命令标签（仅适用于命令完成消息）的
    /// </summary>
    public string command_tag { get; set; } = null!;

    /// <summary>
    ///     获取或设置事务状态（仅适用于就绪消息）的
    /// </summary>
    public PostgreSqlConstants.TransactionStatus? transaction_status { get; set; }

    /// <summary>
    ///     获取或设置字段描述（仅适用于行描述消息）的
    /// </summary>
    public List<PostgreSqlFieldDescription> field_descriptions { get; set; } = [];
}

/// <summary>
///     PostgreSQL 字段描述的
/// </summary>
public class PostgreSqlFieldDescription
{
    /// <summary>
    ///     获取或设置字段名称的
    /// </summary>
    public string name { get; set; } = null!;

    /// <summary>
    ///     获取或设置表 ID的
    /// </summary>
    public uint table_id { get; set; }

    /// <summary>
    ///     获取或设置字的ID的
    /// </summary>
    public ushort column_id { get; set; }

    /// <summary>
    ///     获取或设置数据类的ID的
    /// </summary>
    public uint data_type_oid { get; set; }

    /// <summary>
    ///     获取或设置数据类型大小的
    /// </summary>
    public ushort data_type_size { get; set; }

    /// <summary>
    ///     获取或设置类型修饰符的
    /// </summary>
    public int type_modifier { get; set; }

    /// <summary>
    ///     获取或设置格式代码的
    /// </summary>
    public ushort format_code { get; set; }
}