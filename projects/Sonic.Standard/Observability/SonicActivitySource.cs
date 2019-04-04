using System.Diagnostics;

namespace Std.Observability;

/// <summary>
///     Sonic.Standard 活动（Activity）源，用于创建分布式追踪跨度。
/// </summary>
public static class SonicActivitySource
{
    /// <summary>
    ///     活动（Activity）源实例。
    /// </summary>
    public static readonly ActivitySource activity_source = new("Sonic.Standard.Observability", "1.0.0");

    /// <summary>
    ///     启动一个新的追踪活动。
    /// </summary>
    /// <param name="name">活动名称。</param>
    /// <returns>新创建的活动实例，如果无监听器则返回 null。</returns>
    public static Activity? start_activity(string name)
    {
        return activity_source.StartActivity(name);
    }
}