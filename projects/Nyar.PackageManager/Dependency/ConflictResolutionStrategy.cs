namespace Nyar.PackageManager.Dependency;

/// <summary>
///     冲突解决策略
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>
    ///     未解决
    /// </summary>
    none,

    /// <summary>
    ///     选择最高兼容版本
    /// </summary>
    highest_compatible,

    /// <summary>
    ///     使用 overrides 强制指定版本
    /// </summary>
    @override,

    /// <summary>
    ///     需要用户手动解决
    /// </summary>
    manual
}