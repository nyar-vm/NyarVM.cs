namespace Nyar.PackageRegistry;

/// <summary>
///     版本递增类型
/// </summary>
public enum VersionBump
{
    /// <summary>递增补丁版本号（1.0.0 → 1.0.1）</summary>
    patch,

    /// <summary>递增次版本号（1.0.0 → 1.1.0）</summary>
    minor,

    /// <summary>递增主版本号（1.0.0 → 2.0.0）</summary>
    major
}