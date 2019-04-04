namespace Valhalla;

/// <summary>
///     版本状态
/// </summary>
public enum VersionStatus
{
    /// <summary>活跃可用</summary>
    active,

    /// <summary>已屏蔽（有漏洞）</summary>
    shielded,

    /// <summary>已弃用</summary>
    deprecated
}