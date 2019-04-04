namespace Animator.Abstractions;

/// <summary>
///     动画资源统一契约（原始静态资源，支持多实例复用）
/// </summary>
public interface IAnimationResource
{
    /// <summary>资源唯一标识</summary>
    string resource_identifier { get; }

    /// <summary>动画格式唯一标识</summary>
    string format_identifier { get; }

    /// <summary>内容原始宽度</summary>
    float content_width { get; }

    /// <summary>内容原始高度</summary>
    float content_height { get; }
}