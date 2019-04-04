using System.Text.Json;

namespace Valhalla.Audit;

/// <summary>
///     审计日志存储
/// </summary>
public class AuditLog
{
    private readonly string _audit_file_path;
    private readonly List<AuditEntry> _entries;

    public AuditLog(string auditDirectory)
    {
        _audit_file_path = Path.Combine(auditDirectory, "audit.log");
        _entries = load_entries();
    }

    /// <summary>
    ///     追加一条审计日志
    /// </summary>
    public void append(AuditEntry entry)
    {
        if (string.IsNullOrEmpty(entry.actor)) throw new ArgumentException("Actor 不能为空");

        entry.timestamp = DateTime.UtcNow;
        _entries.Add(entry);
        persist();
    }

    /// <summary>
    ///     记录包发布操作
    /// </summary>
    public void log_publish(string packageName, string version, string publisher,
        string? sha256 = null, Dictionary<string, string>? details = null)
    {
        var entry = new AuditEntry
        {
            operation = AuditOperation.publish,
            package_name = packageName,
            version = version,
            actor = publisher,
            sha256 = sha256,
            details = details ?? new Dictionary<string, string>()
        };
        append(entry);
    }

    /// <summary>
    ///     记录组织注册操作
    /// </summary>
    public void log_register_org(string orgName, string actor, string role)
    {
        var entry = new AuditEntry
        {
            operation = AuditOperation.register_org,
            package_name = orgName,
            actor = actor,
            actor_role = role
        };
        append(entry);
    }

    /// <summary>
    ///     记录包屏蔽操作
    /// </summary>
    public void log_shield(string packageName, string version, string actor,
        string reason)
    {
        var entry = new AuditEntry
        {
            operation = AuditOperation.shield,
            package_name = packageName,
            version = version,
            actor = actor,
            details = new Dictionary<string, string> { ["reason"] = reason }
        };
        append(entry);
    }

    /// <summary>
    ///     记录授权操作
    /// </summary>
    public void log_authorize(string packageName, string actor, string authorizedActor, string role)
    {
        var entry = new AuditEntry
        {
            operation = AuditOperation.authorize,
            package_name = packageName,
            actor = actor,
            details = new Dictionary<string, string>
            {
                ["authorized"] = authorizedActor,
                ["role"] = role
            }
        };
        append(entry);
    }

    /// <summary>
    ///     记录取消授权操作
    /// </summary>
    public void log_revoke_authorization(string packageName, string actor, string revokedActor)
    {
        var entry = new AuditEntry
        {
            operation = AuditOperation.revoke_authorization,
            package_name = packageName,
            actor = actor,
            details = new Dictionary<string, string> { ["revoked"] = revokedActor }
        };
        append(entry);
    }

    /// <summary>
    ///     记录转移发布者操作
    /// </summary>
    public void log_transfer_publisher(string packageName, string fromActor, string toActor)
    {
        var entry = new AuditEntry
        {
            operation = AuditOperation.transfer_publisher,
            package_name = packageName,
            actor = fromActor,
            details = new Dictionary<string, string> { ["to"] = toActor }
        };
        append(entry);
    }

    /// <summary>
    ///     查询指定包的所有审计日志
    /// </summary>
    public List<AuditEntry> query_by_package(string packageName)
    {
        return
        [
            .. _entries
                .Where(e => e.package_name.Equals(packageName, StringComparison.OrdinalIgnoreCase))
        ];
    }

    /// <summary>
    ///     查询指定操作的审计日志
    /// </summary>
    public List<AuditEntry> query_by_operation(AuditOperation operation)
    {
        return [.. _entries.Where(e => e.operation == operation)];
    }

    /// <summary>
    ///     查询指定时间范围内的审计日志
    /// </summary>
    public List<AuditEntry> query_by_time_range(DateTime from, DateTime to)
    {
        return
        [
            .. _entries
                .Where(e => e.timestamp >= from && e.timestamp <= to)
        ];
    }

    /// <summary>
    ///     获取所有审计日志
    /// </summary>
    public IReadOnlyList<AuditEntry> get_all()
    {
        return _entries.AsReadOnly();
    }

    private List<AuditEntry> load_entries()
    {
        if (!File.Exists(_audit_file_path)) return [];

        try
        {
            var json = File.ReadAllText(_audit_file_path);
            return JsonSerializer.Deserialize<List<AuditEntry>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void persist()
    {
        var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_audit_file_path, json);
    }
}