namespace Nyar.VM.NyarVM.HotReload;

/// <summary>
///     模块变更事件参数
/// </summary>
public sealed class ModuleChangedEventArgs : EventArgs
{
    /// <summary>
    ///     变更类型
    /// </summary>
    public required ModuleChangeType change_type { get; init; }

    /// <summary>
    ///     模块名称
    /// </summary>
    public required string module_name { get; init; }

    /// <summary>
    ///     旧模块版本（重载时）
    /// </summary>
    public uint old_version { get; init; }

    /// <summary>
    ///     新模块版本（重载时）
    /// </summary>
    public uint new_version { get; init; }

    /// <summary>
    ///     受影响的依赖模块列表
    /// </summary>
    public IReadOnlyList<string> affected_dependencies { get; init; } = [];
}

/// <summary>
///     模块变更类型
/// </summary>
public enum ModuleChangeType
{
    /// <summary>
    ///     模块加载
    /// </summary>
    loaded,

    /// <summary>
    ///     模块卸载
    /// </summary>
    unloaded,

    /// <summary>
    ///     模块重载（热替换）
    /// </summary>
    reloaded
}