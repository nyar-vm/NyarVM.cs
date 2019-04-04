using Animator.Core;
using Animator.Implementations.Live2D;
using Animator.Implementations.Spine;
using Animator.Implementations.SpriteSheet;

namespace Animator;

/// <summary>
///     动画模块初始化入口（按需注册解析器与工厂）
/// </summary>
public static class AnimatorStartup
{
    /// <summary>
    ///     初始化动画模块，注册所需解析器与工厂
    /// </summary>
    public static void initialize()
    {
        AnimationManager.register_resource_parser(new SpineResourceParser());
        AnimationManager.register_instance_factory(new SpineInstanceFactory());

        AnimationManager.register_resource_parser(new Live2DResourceParser());
        AnimationManager.register_instance_factory(new Live2DInstanceFactory());

        AnimationManager.register_resource_parser(new SpriteSheetResourceParser());
        AnimationManager.register_instance_factory(new SpriteSheetInstanceFactory());
    }
}