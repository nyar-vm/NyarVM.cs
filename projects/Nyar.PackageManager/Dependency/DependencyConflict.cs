using Nyar.PackageManager.Version;

namespace Nyar.PackageManager.Dependency;

public class DependencyConflict
{
    /// <summary>
    ///     冲突的包名称
    /// </summary>
    public string package_name { get; set; } = string.Empty;

    /// <summary>
    ///     请求的版本列表
    /// </summary>
    public List<string> requested_versions { get; set; } = [];

    /// <summary>
    ///     已解决的版本号
    /// </summary>
    public string? resolved_version { get; set; }

    /// <summary>
    ///     冲突解决策略
    /// </summary>
    public ConflictResolutionStrategy resolution_strategy { get; set; }

    /// <summary>
    ///     是否已解决
    /// </summary>
    public bool is_resolved => resolved_version is not null;

    /// <summary>
    ///     是否为严重冲突（无法自动解决）
    /// </summary>
    public bool is_severe
    {
        get
        {
            if (requested_versions.Count < 2) return false;

            var majorVersions = requested_versions
                .Select(v =>
                    SemanticVersion.try_parse(v.TrimStart('^', '~', '>', '<', '='), out var sv) ? sv.major : -1)
                .Where(m => m >= 0)
                .Distinct()
                .ToList();

            return majorVersions.Count > 1;
        }
    }
}