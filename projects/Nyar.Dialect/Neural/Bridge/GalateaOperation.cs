namespace Nyar.Dialect.Neural.Bridge;

/// <summary>
///     Galatea 风格的操作描述，与 Galatea.GraphOperation 对齐。
/// </summary>
public sealed class GalateaOperation
{
    /// <summary>
    ///     操作名称（唯一标识）
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    ///     操作类型字符串（如 "Conv2D", "Dense", "ReLU" 等）
    /// </summary>
    public string OpType { get; init; } = "";

    /// <summary>
    ///     配置参数
    /// </summary>
    public Dictionary<string, object> Config { get; init; } = [];

    /// <summary>
    ///     输入来源名称列表
    /// </summary>
    public string[] InputNames { get; init; } = [];
}