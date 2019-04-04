namespace Nyar.PackageManager.Sdk;

/// <summary>
///     FFI 缁戝畾澹版槑
/// </summary>
public class FfiBinding
{
    /// <summary>
    ///     鍑芥暟鍚?    ///
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     鍑芥暟绛惧悕锛堢被鍨嬬鍚嶈〃杈惧紡锛?    ///
    /// </summary>
    public string signature { get; set; } = string.Empty;

    /// <summary>
    ///     澶栭儴搴撳悕
    /// </summary>
    public string? library { get; set; }

    /// <summary>
    ///     璋冪敤绾﹀畾锛歝decl / stdcall / fastcall
    /// </summary>
    public string? convention { get; set; } = "cdecl";
}