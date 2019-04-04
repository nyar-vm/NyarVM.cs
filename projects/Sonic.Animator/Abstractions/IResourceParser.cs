using System.Threading.Tasks;

namespace Animator.Abstractions;

/// <summary>
///     文件解析器统一契约
/// </summary>
public interface IResourceParser
{
    /// <summary>判断当前解析器是否支持该文件</summary>
    bool can_parse(string filePath);

    /// <summary>异步解析文件为动画资源</summary>
    Task<IAnimationResource> parse(string filePath);
}