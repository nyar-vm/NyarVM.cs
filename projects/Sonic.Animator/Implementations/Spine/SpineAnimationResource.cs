using Animator.Abstractions;
using Animator.Core;

namespace Animator.Implementations.Spine;

/// <summary>
///     Spine 动画资源实现
/// </summary>
internal sealed class SpineAnimationResource : IAnimationResource
{
    public SpineAnimationResource(string identifier, float width, float height)
    {
        resource_identifier = identifier;
        content_width = width;
        content_height = height;
    }

    /// <summary>资源唯一标识</summary>
    public string resource_identifier { get; }

    /// <summary>动画格式唯一标识</summary>
    public string format_identifier => AnimationFormatIdentifier.spine;

    /// <summary>内容原始宽度</summary>
    public float content_width { get; }

    /// <summary>内容原始高度</summary>
    public float content_height { get; }
}