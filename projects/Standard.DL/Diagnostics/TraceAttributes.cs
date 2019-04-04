namespace Std.DL.Diagnostics;

/// <summary>追踪属性</summary>
public sealed class TraceAttributes
{
    /// <summary>属性字典</summary>
    public Dictionary<string, object> Items { get; init; } = new();
}