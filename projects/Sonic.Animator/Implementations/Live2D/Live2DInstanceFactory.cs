using Animator.Abstractions;
using Animator.Core;

namespace Animator.Implementations.Live2D;

/// <summary>
///     Live2D 动画实例工厂
/// </summary>
public sealed class Live2DInstanceFactory : IAnimationInstanceFactory
{
    /// <summary>判断工厂是否匹配当前动画资源</summary>
    public bool match_resource(IAnimationResource resource)
    {
        return resource.format_identifier == AnimationFormatIdentifier.live2_d;
    }

    /// <summary>创建动画运行实例</summary>
    public IAnimationInstance create_instance(IAnimationResource resource)
    {
        return new Live2DAnimationInstance(resource);
    }
}