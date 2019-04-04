namespace Valhalla.Audit;

/// <summary>
///     审计日志操作类型
/// </summary>
public enum AuditOperation
{
    register_org,
    register_package,
    publish,
    authorize,
    revoke_authorization,
    transfer_publisher,
    shield,
    unshield,
    purge
}