using Animator.Abstractions;
using Animator.Core;

namespace Animator.Implementations.SpriteSheet;

/// <summary>
///     序列帧动画资源实现
/// </summary>
internal sealed class SpriteSheetAnimationResource : IAnimationResource
{
    public SpriteSheetAnimationResource(string identifier, float width, float height, int frameCount,
        float framesPerSecond)
    {
        resource_identifier = identifier;
        content_width = width;
        content_height = height;
        frame_count = frameCount;
        frames_per_second = framesPerSecond;
    }

    /// <summary>帧总数</summary>
    public int frame_count { get; }

    /// <summary>每秒帧数</summary>
    public float frames_per_second { get; }

    /// <summary>资源唯一标识</summary>
    public string resource_identifier { get; }

    /// <summary>动画格式唯一标识</summary>
    public string format_identifier => AnimationFormatIdentifier.sprite_sheet;

    /// <summary>内容原始宽度</summary>
    public float content_width { get; }

    /// <summary>内容原始高度</summary>
    public float content_height { get; }
}