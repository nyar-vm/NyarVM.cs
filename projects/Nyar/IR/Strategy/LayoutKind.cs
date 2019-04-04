namespace Nyar.IR.Strategy;

/// <summary>
///     内存数据布局策略
/// </summary>
public enum LayoutKind
{
    /// <summary>Structure of Arrays 布局，适合向量化遍历。</summary>
    so_a,

    /// <summary>Array of Structures 布局，适合缓存局部性。</summary>
    ao_s,

    /// <summary>自动选择最优布局。</summary>
    auto
}