namespace Core.Config;

/// <summary>
///     集合与嵌套对象的合并策略。
/// </summary>
public enum CollectionMergeStrategy
{
    /// <summary>
    ///     整体替换。
    /// </summary>
    overwrite = 0,

    /// <summary>
    ///     追加。
    /// </summary>
    append = 1,

    /// <summary>
    ///     去重追加。
    /// </summary>
    unique_append = 2,

    /// <summary>
    ///     浅层合并。
    /// </summary>
    shallow_merge = 3,

    /// <summary>
    ///     深度合并。
    /// </summary>
    deep_merge = 4
}