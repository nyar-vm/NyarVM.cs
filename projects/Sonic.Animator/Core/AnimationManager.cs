using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Animator.Abstractions;

namespace Animator.Core;

/// <summary>
///     动画全局管理器（注册、加载、实例创建统一入口）
/// </summary>
public static class AnimationManager
{
    private static readonly List<IResourceParser> _resource_parsers = [];
    private static readonly List<IAnimationInstanceFactory> _instance_factories = [];

    #region 实例创建

    /// <summary>创建动画运行实例（自动匹配工厂）</summary>
    public static IAnimationInstance create_animation_instance(IAnimationResource resource)
    {
        foreach (var factory in _instance_factories)
            if (factory.match_resource(resource))
                return factory.create_instance(resource);

        throw new NotSupportedException("未找到对应实例工厂，无法创建动画实例");
    }

    #endregion

    #region 注册能力（外部格式实现主动注册）

    /// <summary>注册资源解析器</summary>
    public static void register_resource_parser(IResourceParser parser)
    {
        if (!_resource_parsers.Contains(parser)) _resource_parsers.Add(parser);
    }

    /// <summary>注册动画实例工厂</summary>
    public static void register_instance_factory(IAnimationInstanceFactory factory)
    {
        if (!_instance_factories.Contains(factory)) _instance_factories.Add(factory);
    }

    #endregion

    #region 资源加载

    /// <summary>异步加载动画资源（自动匹配解析器）</summary>
    public static async Task<IAnimationResource> load_resource(string filePath)
    {
        foreach (var parser in _resource_parsers)
            if (parser.can_parse(filePath))
                return await parser.parse(filePath);

        throw new NotSupportedException("未找到对应解析器，不支持当前文件格式");
    }

    #endregion
}