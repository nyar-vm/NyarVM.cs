namespace Nyar.PackageManager.Version;

internal class VersionRequest
{
    public string raw_spec { get; set; } = string.Empty;
    public string resolved_version { get; set; } = string.Empty;
}