using System.Threading.Tasks;

namespace Core.Security.Audit;

/// <summary>
///     IAuditStore 接口
/// </summary>
public interface IAuditStore
{
    /// <summary>
    ///     异步写入审计记录
    /// </summary>
    Task write(IAuditEntry entry);
}