namespace Animator.Core;

/// <summary>
///     动画播放状态
/// </summary>
public enum PlayStatus
{
    /// <summary>空闲</summary>
    idle,

    /// <summary>播放中</summary>
    playing,

    /// <summary>已暂停</summary>
    paused,

    /// <summary>已停止</summary>
    stopped
}