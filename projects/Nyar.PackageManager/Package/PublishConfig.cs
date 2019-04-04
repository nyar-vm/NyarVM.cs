namespace Nyar.PackageManager.Package;

/// <summary>
///     鍙戝竷閰嶇疆锛屾帶鍒跺寘鍙戝竷鍒版敞鍐岃〃鏃剁殑琛屼负
/// </summary>
public class PublishConfig
{
    /// <summary>
    ///     鐩爣娉ㄥ唽琛ㄥ悕绉帮紙瑕嗙洊榛樿娉ㄥ唽琛級
    /// </summary>
    public string? registry { get; set; }

    /// <summary>
    ///     璁块棶绾у埆锛歱ublic 鎴?restricted
    /// </summary>
    public string access { get; set; } = "public";

    /// <summary>
    ///     鍙戝竷鏍囩锛堥粯璁?latest锛屽彲璁句负 next/beta/rc 绛夛級
    /// </summary>
    public string tag { get; set; } = "latest";
}