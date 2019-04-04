using Nyar.Types;

namespace Nyar.VM.NyarVM.HotReload;

/// <summary>
///     模块状态快照：记录热重载时需要保留的运行时状态
/// </summary>
public sealed class ModuleStateSnapshot
{
    /// <summary>
    ///     初始化模块状态快照
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <param name="version">快照时的模块版本。</param>
    /// <param name="globalValues">全局变量表快照（变量名 → 值）。</param>
    /// <param name="persistentObjects">持久化对象表快照（索引 → 对象）。</param>
    internal ModuleStateSnapshot(
        string moduleName,
        uint version,
        Dictionary<string, Value> globalValues,
        Dictionary<int, object?> persistentObjects)
    {
        module_name = moduleName;
        this.version = version;
        global_values = globalValues;
        persistent_objects = persistentObjects;
    }

    /// <summary>
    ///     模块名称
    /// </summary>
    public string module_name { get; }

    /// <summary>
    ///     快照时的模块版本
    /// </summary>
    public uint version { get; }

    /// <summary>
    ///     全局变量表快照（变量名 → 值）
    /// </summary>
    public Dictionary<string, Value> global_values { get; }

    /// <summary>
    ///     持久化对象表快照（索引 → 对象）
    /// </summary>
    public Dictionary<int, object?> persistent_objects { get; }
}

/// <summary>
///     热重载配置
/// </summary>
public sealed class HotReloadOptions
{
    /// <summary>
    ///     热重载配置默认值
    /// </summary>
    public static HotReloadOptions @default { get; } = new()
    {
        preserve_globals = true,
        preserve_objects = true,
        invalidate_active_frames = false,
        strict_version_check = true
    };

    /// <summary>
    ///     是否保留全局变量状态（推荐开启，避免全局状态丢失）
    /// </summary>
    public bool preserve_globals { get; init; } = true;

    /// <summary>
    ///     是否保留持久化对象（堆上分配的对象，推荐开启）
    /// </summary>
    public bool preserve_objects { get; init; } = true;

    /// <summary>
    ///     重载时是否使活跃帧失效（false = 允许旧帧继续执行直到自然返回）
    /// </summary>
    public bool invalidate_active_frames { get; init; }

    /// <summary>
    ///     是否启用严格版本检查（检查函数签名兼容性）
    /// </summary>
    public bool strict_version_check { get; init; } = true;
}

/// <summary>
///     模块版本兼容性检查结果
/// </summary>
public sealed class CompatibilityCheckResult
{
    /// <summary>
    ///     初始化兼容性检查结果
    /// </summary>
    public required bool is_compatible { get; init; }

    /// <summary>
    ///     不兼容的函数列表
    /// </summary>
    public required List<IncompatibleFunction> incompatible_functions { get; init; }

    /// <summary>
    ///     错误消息（如果不兼容）
    /// </summary>
    public string? error_message { get; init; }
}

/// <summary>
///     不兼容的函数信息
/// </summary>
public sealed class IncompatibleFunction
{
    /// <summary>
    ///     函数名称
    /// </summary>
    public required string function_name { get; init; }

    /// <summary>
    ///     不兼容原因
    /// </summary>
    public required string reason { get; init; }

    /// <summary>
    ///     旧函数签名
    /// </summary>
    public required string old_signature { get; init; }

    /// <summary>
    ///     新函数签名
    /// </summary>
    public required string new_signature { get; init; }
}