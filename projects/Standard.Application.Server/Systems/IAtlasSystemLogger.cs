namespace Std.App.Server.Systems;

/// <summary>
///     Atlas 系统日志接口，支持结构化单行日志。
///     格式：[系统名:动作] Key1=Value1; Key2=Value2; ...
/// </summary>
public interface IAtlasSystemLogger
{
    /// <summary>
    ///     记录信息日志
    /// </summary>
    /// <param name="action">动作名称</param>
    /// <param name="keyValues">键值对参数</param>
    void Log(string action, params object[] keyValues);

    /// <summary>
    ///     记录警告日志
    /// </summary>
    /// <param name="action">动作名称</param>
    /// <param name="keyValues">键值对参数</param>
    void LogWarning(string action, params object[] keyValues);

    /// <summary>
    ///     记录错误日志
    /// </summary>
    /// <param name="action">动作名称</param>
    /// <param name="ex">异常对象</param>
    /// <param name="keyValues">键值对参数</param>
    void LogError(string action, Exception? ex, params object[] keyValues);
}