namespace Valkyrie.Asgard.Effect;

/// <summary>
///     Effect 状态枚举
/// </summary>
public enum EffectStatus
{
    /// <summary>等待中</summary>
    pending,

    /// <summary>已解决</summary>
    resolved,

    /// <summary>已拒绝</summary>
    rejected
}
