namespace Animator.Abstractions;

/// <summary>
///     动画实例工厂契约（根据资源匹配并创建对应运行实例）
/// </summary>
public interface IAnimationInstanceFactory
{
    /// <summary>判断工厂是否匹配当前动画资源</summary>
    bool match_resource(IAnimationResource resource);

    /// <summary>创建动画运行实例</summary>
    IAnimationInstance create_instance(IAnimationResource resource);
}