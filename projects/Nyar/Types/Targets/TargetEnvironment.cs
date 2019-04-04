namespace Nyar.Types.Targets;

/// <summary>
///     目标运行环境类型
/// </summary>
public enum TargetEnvironment
{
    /// <summary>
    ///     Web 环境（浏览器）
    /// </summary>
    web,

    /// <summary>
    ///     移动端环境（Android / iOS）
    /// </summary>
    mobile,

    /// <summary>
    ///     原生桌面环境
    /// </summary>
    native
}