using System.Text.Json;
using Valhalla.Audit;

namespace Valhalla;

/// <summary>
///     包注册中心 API 端点
/// </summary>
public class RegistryApi
{
    private readonly AuditLog _audit_log;
    private readonly string _registry_root;

    public RegistryApi(string registryRoot, AuditLog auditLog)
    {
        _registry_root = registryRoot;
        _audit_log = auditLog;
    }

    /// <summary>
    ///     获取所有包列表
    /// </summary>
    public List<string> list_packages()
    {
        var manifestDir = Path.Combine(_registry_root, "manifests");
        if (!Directory.Exists(manifestDir)) return [];

        return
        [
            .. Directory.GetFiles(manifestDir, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f)!)
        ];
    }

    /// <summary>
    ///     获取包清单
    /// </summary>
    public PackageManifest? get_package(string name)
    {
        var path = Path.Combine(_registry_root, "manifests", $"{name}.json");
        if (!File.Exists(path)) return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<PackageManifest>(json);
    }

    /// <summary>
    ///     注册新包
    /// </summary>
    public bool register_package(PackageManifest manifest, string actor)
    {
        var existing = get_package(manifest.name);
        if (existing is not null) return false;

        var path = Path.Combine(_registry_root, "manifests", $"{manifest.name}.json");
        manifest.registered_at = DateTime.UtcNow;
        manifest.incarnation = 1;

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        _audit_log.append(new AuditEntry
        {
            operation = AuditOperation.register_package,
            package_name = manifest.name,
            actor = actor,
            details = new Dictionary<string, string> { ["publisher"] = manifest.publisher }
        });

        return true;
    }

    /// <summary>
    ///     发布包版本
    /// </summary>
    public bool publish_version(string packageName, VersionEntry version, string actor,
        string? signature = null)
    {
        var manifest = get_package(packageName);
        if (manifest is null) return false;

        if (!manifest.versions.TryAdd(version.version, version)) return false;

        manifest.incarnation++;

        var path = Path.Combine(_registry_root, "manifests", $"{packageName}.json");
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        _audit_log.log_publish(packageName, version.version, actor,
            version.package_digest);

        return true;
    }

    /// <summary>
    ///     查询包的审计日志
    /// </summary>
    public List<AuditEntry> get_audit_log(string packageName)
    {
        return _audit_log.query_by_package(packageName);
    }

    /// <summary>
    ///     搜索包（按名称前缀）
    /// </summary>
    public List<PackageSearchResult> search(string query)
    {
        var results = new List<PackageSearchResult>();
        foreach (var name in list_packages())
        {
            if (!name.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;

            var manifest = get_package(name);
            if (manifest is null || manifest.status == PackageStatus.purged) continue;

            var latest = manifest.versions.Values
                .OrderByDescending(v => v.published_at)
                .FirstOrDefault();

            results.Add(new PackageSearchResult
            {
                name = name,
                description = latest?.package_digest ?? string.Empty,
                latest_version = latest?.version ?? "0.0.0",
                published_at = latest?.published_at ?? DateTime.MinValue
            });
        }

        return results;
    }

    /// <summary>
    ///     屏蔽指定版本
    /// </summary>
    public bool shield_version(string packageName, string version, string reason, string actor)
    {
        var manifest = get_package(packageName);
        if (manifest is null || !manifest.versions.TryGetValue(version, out var entry)) return false;

        entry.status = VersionStatus.shielded;
        entry.shield_reason = reason;
        entry.shielded_at = DateTime.UtcNow;

        var path = Path.Combine(_registry_root, "manifests", $"{packageName}.json");
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        _audit_log.log_shield(packageName, version, actor, reason);
        return true;
    }
}