using Core.Security.Audit;

namespace Std.Security.Audit;

/// <summary>
///     审计上下文，提供审计记录写入能力。
/// </summary>
public static class AuditContext
{
    private static IAuditStore? _store;

    /// <summary>
    ///     写入审计记录到当前配置的审计存储。
    /// </summary>
    /// <param name="entry">审计记录。</param>
    public static void write(IAuditEntry entry)
    {
        _store?.write(entry).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     设置审计存储实现。
    /// </summary>
    /// <param name="store">审计存储。</param>
    public static void set_store(IAuditStore? store)
    {
        _store = store;
    }
}