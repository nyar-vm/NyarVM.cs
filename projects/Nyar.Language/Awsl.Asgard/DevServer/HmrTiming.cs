namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     HMR 延迟测量记录，用于跟踪热重载全链路性能
/// </summary>
public readonly struct HmrTiming
{
    /// <summary>文件变更检测时间</summary>
    public DateTime file_changed_at { get; init; }

    /// <summary>编译开始时间</summary>
    public DateTime? compile_start_at { get; init; }

    /// <summary>编译完成时间</summary>
    public DateTime? compile_end_at { get; init; }

    /// <summary>广播发送完成时间</summary>
    public DateTime? broadcast_at { get; init; }

    /// <summary>变更的文件路径</summary>
    public string file_path { get; init; }

    /// <summary>从文件变更到编译开始的延迟</summary>
    public double detect_ms => compile_start_at.HasValue
        ? (compile_start_at.Value - file_changed_at).TotalMilliseconds
        : 0;

    /// <summary>编译耗时</summary>
    public double compile_ms => compile_end_at.HasValue && compile_start_at.HasValue
        ? (compile_end_at.Value - compile_start_at.Value).TotalMilliseconds
        : 0;

    /// <summary>从文件变更到广播完成的总延迟</summary>
    public double total_ms => broadcast_at.HasValue
        ? (broadcast_at.Value - file_changed_at).TotalMilliseconds
        : 0;
}
