namespace Nyar.Optimizer;

/// <summary>
///     空值类型，表示无分析数据
/// </summary>
public sealed class Unit
{
    private Unit()
    {
    }

    /// <summary>
    ///     单例实例
    /// </summary>
    public static Unit instance { get; } = new();
}