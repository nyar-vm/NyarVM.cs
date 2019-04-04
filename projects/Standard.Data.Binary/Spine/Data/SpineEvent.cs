namespace Std.Data.Binary.Spine.Data;

/// <summary>
///     Spine 事件数据的
/// </summary>
public sealed class SpineEvent
{
    /// <summary>
    ///     事件名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     整数值的
    /// </summary>
    public int? int_value { get; init; }

    /// <summary>
    ///     浮点值的
    /// </summary>
    public float? float_value { get; init; }

    /// <summary>
    ///     字符串值的
    /// </summary>
    public string? string_value { get; init; }

    /// <summary>
    ///     音频路径的
    /// </summary>
    public string? audio_path { get; init; }
}