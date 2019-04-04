namespace Nyar.PackageManager.Sdk;

/// <summary>
///     SDK 骞冲彴瑕佹眰
/// </summary>
public class SdkPlatformRequirement
{
    /// <summary>
    ///     鏈€浣庡钩鍙扮増鏈?    ///
    /// </summary>
    public string? min_version { get; set; }

    /// <summary>
    ///     鏈€楂樺钩鍙扮増鏈?    ///
    /// </summary>
    public string? max_version { get; set; }

    /// <summary>
    ///     鎿嶄綔绯荤粺瑕佹眰
    /// </summary>
    public string? os { get; set; }

    /// <summary>
    ///     鏋舵瀯瑕佹眰
    /// </summary>
    public string? arch { get; set; }
}