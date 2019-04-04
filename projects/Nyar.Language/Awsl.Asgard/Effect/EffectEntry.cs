using System.Text.Json;

namespace Valkyrie.Asgard.Effect;

/// <summary>
///     Effect 条目，记录一次副作用的完整生命周期
/// </summary>
public sealed record EffectEntry
{
    /// <summary>Effect 唯一标识</summary>
    public string id { get; set; } = string.Empty;

    /// <summary>副作用函数名</summary>
    public string function_name { get; set; } = string.Empty;

    /// <summary>函数参数</summary>
    public string[] args { get; set; } = [];

    /// <summary>当前状态</summary>
    public EffectStatus status { get; set; } = EffectStatus.pending;

    /// <summary>解决结果（仅 Resolved 状态有效）</summary>
    public JsonElement? result { get; set; }

    /// <summary>拒绝原因（仅 Rejected 状态有效）</summary>
    public string error { get; set; } = string.Empty;

    /// <summary>创建时间戳（毫秒）</summary>
    public long created_at { get; set; }

    /// <summary>最后更新时间戳（毫秒）</summary>
    public long updated_at { get; set; }
}
