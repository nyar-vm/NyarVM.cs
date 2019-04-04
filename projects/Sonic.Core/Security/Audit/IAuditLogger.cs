namespace Core.Security.Audit;

/// <summary>
///     审计日志记录器接口
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    ///     记录审计日志
    /// </summary>
    /// <param name="action">操作动作</param>
    /// <param name="user">操作用户</param>
    /// <param name="details">操作详情</param>
    void log(string action, string? user = null, object? details = null);
}