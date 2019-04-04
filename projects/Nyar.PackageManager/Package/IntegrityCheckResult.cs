namespace Nyar.PackageManager.Package;

/// <summary>
///     瀹屾暣鎬ф牎楠岀粨鏋?///
/// </summary>
public class IntegrityCheckResult
{
    /// <summary>
    ///     鍖呭悕绉?    ///
    /// </summary>
    public string package_name { get; set; } = string.Empty;

    /// <summary>
    ///     鐗堟湰鍙?    ///
    /// </summary>
    public string version { get; set; } = string.Empty;

    /// <summary>
    ///     鏍￠獙闂绫诲瀷
    /// </summary>
    public IntegrityIssue issue { get; set; }

    /// <summary>
    ///     闂鎻忚堪
    /// </summary>
    public string message { get; set; } = string.Empty;
}