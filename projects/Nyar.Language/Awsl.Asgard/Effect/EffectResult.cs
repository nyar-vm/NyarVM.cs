using System.Text.Json;

namespace Valkyrie.Asgard.Effect;

/// <summary>
///     Effect 副作用结果
/// </summary>
public sealed class EffectResult
{
    /// <summary>当前状态</summary>
    public EffectStatus status { get; set; } = EffectStatus.pending;

    /// <summary>解决结果数据</summary>
    public JsonElement? data { get; set; }

    /// <summary>拒绝原因</summary>
    public string? error { get; set; }

    /// <summary>Effect 唯一标识</summary>
    public string entry_id { get; set; } = string.Empty;
}
