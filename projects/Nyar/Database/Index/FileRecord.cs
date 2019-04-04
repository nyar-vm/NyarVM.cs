namespace Nyar.Database.Index;

/// <summary>
///     文件索引记录
/// </summary>
public sealed record FileRecord
{
    /// <summary>
    ///     文件 URI
    /// </summary>
    public string uri { get; init; } = "";

    /// <summary>
    ///     文件内容的 SHA256 哈希值（十六进制字符串）
    /// </summary>
    public string content_hash { get; init; } = "";

    /// <summary>
    ///     文件最后修改时间（UTC）
    /// </summary>
    public DateTime last_modified { get; init; }

    /// <summary>
    ///     该文件依赖的其他文件 URI 列表
    /// </summary>
    public IReadOnlyList<string> dependency_uris { get; init; } = [];

    /// <summary>
    ///     文件的语言标识（如 "typescript", "rust"）
    /// </summary>
    public string language_id { get; init; } = "";

    /// <summary>
    ///     文件分析状态
    /// </summary>
    public FileAnalysisStatus analysis_status { get; init; }
}