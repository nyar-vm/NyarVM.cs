namespace Std.Data.Binary.MySQL.Data;

/// <summary>
///     MySQL 协议常量的
/// </summary>
public static class MySqlConstants
{
    /// <summary>
    ///     命令类型的
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        ///     退出命令的
        /// </summary>
        quit = 0x01,

        /// <summary>
        ///     初始化数据库命令的
        /// </summary>
        init_db = 0x02,

        /// <summary>
        ///     查询命令的
        /// </summary>
        query = 0x03,

        /// <summary>
        ///     字段列表命令的
        /// </summary>
        field_list = 0x04,

        /// <summary>
        ///     创建数据库命令的
        /// </summary>
        create_db = 0x05,

        /// <summary>
        ///     删除数据库命令的
        /// </summary>
        drop_db = 0x06,

        /// <summary>
        ///     刷新命令的
        /// </summary>
        refresh = 0x07,

        /// <summary>
        ///     关闭命令的
        /// </summary>
        shutdown = 0x08,

        /// <summary>
        ///     统计命令的
        /// </summary>
        statistics = 0x09,

        /// <summary>
        ///     进程信息命令的
        /// </summary>
        process_info = 0x0a,

        /// <summary>
        ///     连接跟踪命令的
        /// </summary>
        connect = 0x0b,

        /// <summary>
        ///     杀死命令的
        /// </summary>
        kill = 0x0c,

        /// <summary>
        ///     调试命令的
        /// </summary>
        debug = 0x0d,

        /// <summary>
        ///     预编译命令的
        /// </summary>
        ping = 0x0e,

        /// <summary>
        ///     时间命令的
        /// </summary>
        time = 0x0f,

        /// <summary>
        ///     延迟命令的
        /// </summary>
        delayed_insert = 0x10,

        /// <summary>
        ///     更改用户命令的
        /// </summary>
        change_user = 0x11,

        /// <summary>
        ///     二进制日志命令的
        /// </summary>
        binlog_dump = 0x12,

        /// <summary>
        ///     表转储命令的
        /// </summary>
        table_dump = 0x13,

        /// <summary>
        ///     连接关闭命令的
        /// </summary>
        connect_out = 0x14,

        /// <summary>
        ///     注册命令的
        /// </summary>
        register_slave = 0x15,

        /// <summary>
        ///     准备语句命令的
        /// </summary>
        stmt_prepare = 0x16,

        /// <summary>
        ///     执行语句命令的
        /// </summary>
        stmt_execute = 0x17,

        /// <summary>
        ///     发送长数据命令的
        /// </summary>
        stmt_send_long_data = 0x18,

        /// <summary>
        ///     关闭语句命令的
        /// </summary>
        stmt_close = 0x19,

        /// <summary>
        ///     重置语句命令的
        /// </summary>
        stmt_reset = 0x1a,

        /// <summary>
        ///     设置选项命令的
        /// </summary>
        set_option = 0x1b,

        /// <summary>
        ///     语句获取命令的
        /// </summary>
        stmt_fetch = 0x1c,

        /// <summary>
        ///     守护进程命令的
        /// </summary>
        daemon = 0x1d,

        /// <summary>
        ///     二进制日志转的GTID 命令的
        /// </summary>
        binlog_dump_gtid = 0x1e,

        /// <summary>
        ///     重置连接命令的
        /// </summary>
        reset_connection = 0x1f
    }

    /// <summary>
    ///     错误码的
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        ///     成功的
        /// </summary>
        success = 0,

        /// <summary>
        ///     访问被拒绝的
        /// </summary>
        access_denied = 1045,

        /// <summary>
        ///     数据库不存在的
        /// </summary>
        database_not_found = 1049,

        /// <summary>
        ///     表不存在的
        /// </summary>
        table_not_found = 1146
    }

    /// <summary>
    ///     服务器状态标志的
    /// </summary>
    [Flags]
    public enum ServerStatus
    {
        /// <summary>
        ///     状态正常的
        /// </summary>
        normal = 0,

        /// <summary>
        ///     服务器处于自动提交模式的
        /// </summary>
        auto_commit = 1 << 0,

        /// <summary>
        ///     服务器处于事务中的
        /// </summary>
        in_transaction = 1 << 1,

        /// <summary>
        ///     结果集已完成的
        /// </summary>
        more_results_exists = 1 << 2
    }

    /// <summary>
    ///     MySQL 协议版本的
    /// </summary>
    public const int protocol_version = 10;

    /// <summary>
    ///     数据包首字节 的OK 包标记的
    /// </summary>
    public const byte packet_marker_ok = 0x00;

    /// <summary>
    ///     数据包首字节 的错误包标记的
    /// </summary>
    public const byte packet_marker_error = 0xFF;

    /// <summary>
    ///     数据包首字节 的EOF 包标记的
    /// </summary>
    public const byte packet_marker_eof = 0xFE;

    /// <summary>
    ///     长度编码整数 的NULL 标记的
    /// </summary>
    public const byte length_encoded_null = 0xFB;

    /// <summary>
    ///     长度编码整数 的UInt16 长度前缀的
    /// </summary>
    public const byte length_encoded_int16 = 0xFC;

    /// <summary>
    ///     长度编码整数 的UInt16 最大值的下一值（的Int24 编码阈值）的
    /// </summary>
    public const int length_encoded_int16_threshold = 0x10000;

    /// <summary>
    ///     长度编码整数 的UInt24 长度前缀的
    /// </summary>
    public const byte length_encoded_int24 = 0xFD;

    /// <summary>
    ///     长度编码整数 的单字节最大值（含）的
    /// </summary>
    public const byte length_encoded_max_single = 0xFA;

    /// <summary>
    ///     命令类型 的最小值的
    /// </summary>
    public const byte command_type_min = 0x01;

    /// <summary>
    ///     命令类型 的最大值的
    /// </summary>
    public const byte command_type_max = 0x1F;

    /// <summary>
    ///     MySQL 列类型（MYSQL_TYPE_*），用于 ColumnDefinition41 包的
    /// </summary>
    public static class ColumnType
    {
        public const byte @decimal = 0x00;
        public const byte tiny = 0x01;
        public const byte @short = 0x02;
        public const byte @long = 0x03;
        public const byte @float = 0x04;
        public const byte @double = 0x05;
        public const byte @null = 0x06;
        public const byte timestamp = 0x07;
        public const byte long_long = 0x08;
        public const byte int24 = 0x09;
        public const byte date = 0x0A;
        public const byte time = 0x0B;
        public const byte date_time = 0x0C;
        public const byte year = 0x0D;
        public const byte var_char = 0x0F;
        public const byte bit = 0x10;
        public const byte json = 0xF5;
        public const byte new_decimal = 0xF6;
        public const byte @enum = 0xF7;
        public const byte set = 0xF8;
        public const byte tiny_blob = 0xF9;
        public const byte medium_blob = 0xFA;
        public const byte long_blob = 0xFB;
        public const byte blob = 0xFC;
        public const byte var_string = 0xFD;
        public const byte @string = 0xFE;
        public const byte geometry = 0xFF;
    }

    /// <summary>
    ///     MySQL 列标志，用于 ColumnDefinition41 包的
    /// </summary>
    public static class ColumnFlag
    {
        public const ushort not_null = 0x0001;
        public const ushort primary_key = 0x0002;
        public const ushort unique_key = 0x0004;
        public const ushort multiple_key = 0x0008;
        public const ushort blob = 0x0010;
        public const ushort unsigned = 0x0020;
        public const ushort zero_fill = 0x0040;
        public const ushort binary = 0x0080;
        public const ushort @enum = 0x0100;
        public const ushort auto_increment = 0x0200;
        public const ushort timestamp = 0x0400;
        public const ushort set = 0x0800;
        public const ushort no_default_value = 0x1000;
    }
}