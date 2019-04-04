namespace Std.Data.Binary.PostgreSQL.Data;

/// <summary>
///     PostgreSQL 协议常量的
/// </summary>
public static class PostgreSqlConstants
{
    /// <summary>
    ///     认证类型的
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        ///     认证成功的
        /// </summary>
        authentication_ok = 0,

        /// <summary>
        ///     认证 Kerberos V5的
        /// </summary>
        authentication_kerberos_v5 = 2,

        /// <summary>
        ///     认证密码的
        /// </summary>
        authentication_cleartext_password = 3,

        /// <summary>
        ///     认证密码 MD5的
        /// </summary>
        authentication_md5_password = 5,

        /// <summary>
        ///     认证 SCM 凭证的
        /// </summary>
        authentication_scm_credential = 6,

        /// <summary>
        ///     认证 GSS的
        /// </summary>
        authentication_gss = 7,

        /// <summary>
        ///     认证 GSS 继续的
        /// </summary>
        authentication_gss_continue = 8,

        /// <summary>
        ///     认证 SSPI的
        /// </summary>
        authentication_sspi = 9,

        /// <summary>
        ///     认证 SASL的
        /// </summary>
        authentication_sasl = 10,

        /// <summary>
        ///     认证 SASL 继续的
        /// </summary>
        authentication_sasl_continue = 11,

        /// <summary>
        ///     认证 SASL 最终的
        /// </summary>
        authentication_sasl_final = 12
    }

    /// <summary>
    ///     消息类型的
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        ///     认证请求的
        /// </summary>
        authentication_request = 'R',

        /// <summary>
        ///     后端键数据的
        /// </summary>
        backend_key_data = 'K',

        /// <summary>
        ///     绑定完成的
        /// </summary>
        bind_complete = '2',

        /// <summary>
        ///     关闭完成的
        /// </summary>
        close_complete = '3',

        /// <summary>
        ///     命令完成的
        /// </summary>
        command_complete = 'C',

        /// <summary>
        ///     复制数据的
        /// </summary>
        copy_data = 'd',

        /// <summary>
        ///     复制完成的
        /// </summary>
        copy_done = 'c',

        /// <summary>
        ///     复制失败的
        /// </summary>
        copy_fail = 'f',

        /// <summary>
        ///     数据行的
        /// </summary>
        data_row = 'D',

        /// <summary>
        ///     错误响应的
        /// </summary>
        error_response = 'E',

        /// <summary>
        ///     空查询响应的
        /// </summary>
        empty_query_response = 'I',

        /// <summary>
        ///     函数调用完成的
        /// </summary>
        function_call_complete = 'V',

        /// <summary>
        ///     行描述的
        /// </summary>
        row_description = 'T',

        /// <summary>
        ///     通知响应的
        /// </summary>
        notification_response = 'A',

        /// <summary>
        ///     参数描述的
        /// </summary>
        parameter_description = 't',

        /// <summary>
        ///     参数状态的
        /// </summary>
        parameter_status = 'S',

        /// <summary>
        ///     解析完成的
        /// </summary>
        parse_complete = '1',

        /// <summary>
        ///     门户消息的
        /// </summary>
        portal_suspended = 's',

        /// <summary>
        ///     就绪消息的
        /// </summary>
        ready_for_query = 'Z',

        /// <summary>
        ///     行描述的
        /// </summary>
        row_description_message = 'T',

        /// <summary>
        ///     同步消息的
        /// </summary>
        sync = 'S',

        /// <summary>
        ///     终止消息的
        /// </summary>
        terminate = 'X',

        /// <summary>
        ///     绑定消息的
        /// </summary>
        bind = 'B',

        /// <summary>
        ///     关闭消息的
        /// </summary>
        close = 'C',

        /// <summary>
        ///     复制失败消息的
        /// </summary>
        copy_fail_message = 'f',

        /// <summary>
        ///     复制数据消息的
        /// </summary>
        copy_data_message = 'd',

        /// <summary>
        ///     复制完成消息的
        /// </summary>
        copy_done_message = 'c',

        /// <summary>
        ///     复制模式消息的
        /// </summary>
        copy_mode = 'H',

        /// <summary>
        ///     执行消息的
        /// </summary>
        execute = 'E',

        /// <summary>
        ///     函数调用消息的
        /// </summary>
        function_call = 'F',

        /// <summary>
        ///     解析消息的
        /// </summary>
        parse = 'P',

        /// <summary>
        ///     查询消息的
        /// </summary>
        query = 'Q',

        /// <summary>
        ///     同步消息的
        /// </summary>
        sync_message = 'S',

        /// <summary>
        ///     终止消息的
        /// </summary>
        terminate_message = 'X'
    }

    /// <summary>
    ///     事务状态的
    /// </summary>
    public enum TransactionStatus
    {
        idle = 'I',
        in_transaction = 'T',
        failed_transaction = 'E'
    }

    /// <summary>
    ///     PostgreSQL 协议版本的
    /// </summary>
    public const int protocol_version = 196608;

    /// <summary>
    ///     SSL 请求的协议版本号的0877103 = 0x04D2162F）的
    ///     客户端在启动阶段发送此特殊版本号来请求 SSL 加密的
    /// </summary>
    public const int ssl_request_code = 80877103;
}