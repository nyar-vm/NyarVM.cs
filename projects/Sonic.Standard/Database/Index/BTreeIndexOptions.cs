namespace Std.Database.Index;

/// <summary>
///     B+ 树索引引擎配置选项（避免与 Sonic.Core 的 IndexOptions 冲突）
/// </summary>
internal sealed class BTreeIndexOptions
{
    /// <summary>
    ///     B+ 树阶数
    /// </summary>
    public int b_tree_order { get; set; } = 128;

    /// <summary>
    ///     默认配置
    /// </summary>
    public static BTreeIndexOptions @default => new();
}