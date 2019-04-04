namespace Nyar.PackageManager.Package;

/// <summary>
///     鐢熷懡鍛ㄦ湡閽╁瓙瀹氫箟锛岀敤浜庡湪鐗瑰畾鏋勫缓闃舵鎵ц鑷畾涔夊懡浠?///
/// </summary>
public class LifecycleHook
{
    /// <summary>
    ///     瑕佹墽琛岀殑鍛戒护
    /// </summary>
    public string command { get; set; } = string.Empty;

    /// <summary>
    ///     鎵ц澶辫触鏃舵槸鍚︿腑姝㈡瀯寤?    ///
    /// </summary>
    public bool fail_on_error { get; set; } = true;

    /// <summary>
    ///     浣跨敤鐨?Shell锛坧wsh / cmd / bash锛夛紝绌哄垯浣跨敤绯荤粺榛樿
    /// </summary>
    public string? shell { get; set; }

    /// <summary>
    ///     骞冲彴鏉′欢琛ㄨ揪寮忥紝婊¤冻鏉′欢鏃舵墠鎵ц锛堝 "windows"銆?!linux"锛?    ///
    /// </summary>
    public string? condition { get; set; }

    /// <summary>
    ///     閽╁瓙鎻忚堪
    /// </summary>
    public string? description { get; set; }
}