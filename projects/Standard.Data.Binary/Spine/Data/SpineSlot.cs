namespace Std.Data.Binary.Spine.Data;

/// <summary>
///     Spine 插槽数据的
/// </summary>
public sealed class SpineSlot
{
    /// <summary>
    ///     插槽名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     所属骨骼名称的
    /// </summary>
    public string bone { get; init; } = string.Empty;

    /// <summary>
    ///     默认附件名称的
    /// </summary>
    public string? attachment { get; init; }

    /// <summary>
    ///     混合模式的
    /// </summary>
    public string? blend { get; init; }

    /// <summary>
    ///     颜色值（RGBA 十六进制字符串）的
    /// </summary>
    public string? color { get; init; }

    /// <summary>
    ///     暗色值（RGBA 十六进制字符串）的
    /// </summary>
    public string? dark_color { get; init; }
}