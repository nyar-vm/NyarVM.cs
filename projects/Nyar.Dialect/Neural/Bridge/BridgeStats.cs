namespace Nyar.Dialect.Neural.Bridge;

/// <summary>
///     桥接转换统计信息
/// </summary>
public sealed class BridgeStats
{
    /// <summary>
    ///     总操作数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    ///     成功转换的操作数
    /// </summary>
    public int ConvertedCount { get; set; }

    /// <summary>
    ///     跳过的操作数（不支持的 OpType）
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    ///     转换率
    /// </summary>
    public double ConversionRate => TotalCount > 0 ? (double)ConvertedCount / TotalCount : 0;
}