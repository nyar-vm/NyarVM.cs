namespace Valhalla.Audit;

/// <summary>
///     单条审计日志条目
/// </summary>
public class AuditEntry
{
    public DateTime timestamp { get; set; } = DateTime.UtcNow;
    public AuditOperation operation { get; set; }
    public string package_name { get; set; } = string.Empty;
    public string? version { get; set; }
    public string? sha256 { get; set; }
    public string actor { get; set; } = string.Empty;
    public string actor_role { get; set; } = "publisher";
    public Dictionary<string, string> details { get; set; } = new();
    public string signature { get; set; } = string.Empty;
}