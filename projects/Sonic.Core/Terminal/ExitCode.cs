namespace Core.Terminal;

/// <summary>
///     定义命令执行的标准退出码。
/// </summary>
public enum ExitCode
{
    /// <summary>
    ///     执行成功。
    /// </summary>
    Success = 0,

    /// <summary>
    ///     执行出错。
    /// </summary>
    Error = 1,

    /// <summary>
    ///     无效参数。
    /// </summary>
    InvalidArgs = 2,

    /// <summary>
    ///     命令未找到。
    /// </summary>
    CommandNotFound = 3,

    /// <summary>
    ///     已取消。
    /// </summary>
    Cancelled = 4,

    /// <summary>
    ///     文件未找到。
    /// </summary>
    FileNotFound = 10,

    /// <summary>
    ///     权限被拒绝。
    /// </summary>
    PermissionDenied = 11,

    /// <summary>
    ///     网络错误。
    /// </summary>
    NetworkError = 20,

    /// <summary>
    ///     配置错误。
    /// </summary>
    ConfigurationError = 30,

    /// <summary>
    ///     超时。
    /// </summary>
    Timeout = 40,

    /// <summary>
    ///     未处理的异常。
    /// </summary>
    UnhandledException = 99,

    /// <summary>
    ///     被信号中断。
    /// </summary>
    Interrupted = 130
}