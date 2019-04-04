using Animator.Abstractions;
using Animator.Core;

namespace Animator.Implementations.SpriteSheet;

/// <summary>
///     序列帧动画实例工厂
/// </summary>
public sealed class SpriteSheetInstanceFactory : IAnimationInstanceFactory
{
    /// <summary>判断工厂是否匹配当前动画资源</summary>
    public bool match_resource(IAnimationResource resource)
    {
        return resource.format_identifier == AnimationFormatIdentifier.sprite_sheet;
    }

    /// <summary>创建动画运行实例</summary>
    public IAnimationInstance create_instance(IAnimationResource resource)
    {
        return new SpriteSheetAnimationInstance(resource);
    }
}