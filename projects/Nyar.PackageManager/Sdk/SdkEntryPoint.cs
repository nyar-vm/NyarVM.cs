namespace Nyar.PackageManager.Sdk;

/// <summary>
///     SDK 鍏ュ彛鐐瑰畾涔?///
/// </summary>
public class SdkEntryPoint
{
    /// <summary>
    ///     鍏ュ彛鐐瑰悕绉?    ///
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     鍏ュ彛妯″潡璺緞
    /// </summary>
    public string module { get; set; } = string.Empty;

    /// <summary>
    ///     鍏ュ彛鐐规弿杩?    ///
    /// </summary>
    public string? description { get; set; }
}