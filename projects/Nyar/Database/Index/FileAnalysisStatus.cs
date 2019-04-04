namespace Nyar.Database.Index;

/// <summary>
///     文件分析状态
/// </summary>
public enum FileAnalysisStatus
{
    /// <summary>
    ///     未分析
    /// </summary>
    pending,

    /// <summary>
    ///     正在分析
    /// </summary>
    analyzing,

    /// <summary>
    ///     分析完成
    /// </summary>
    completed,

    /// <summary>
    ///     分析失败
    /// </summary>
    failed,

    /// <summary>
    ///     已过期，需要重新分析
    /// </summary>
    stale
}