using System.Collections.Generic;
using System.Threading.Tasks;
using Animator.Abstractions;
using Animator.Core;

namespace Animator.Extensions;

/// <summary>
///     资源批量管理器
/// </summary>
public sealed class ResourceBatchManager
{
    private readonly Dictionary<string, IAnimationResource> _resources = new();

    /// <summary>
    ///     已加载资源数量
    /// </summary>
    public int resource_count => _resources.Count;

    /// <summary>
    ///     异步批量加载资源
    /// </summary>
    /// <param name="filePaths">文件路径列表</param>
    /// <returns>成功加载的资源数量</returns>
    public async Task<int> load_resources(IReadOnlyList<string> filePaths)
    {
        var loadedCount = 0;

        foreach (var filePath in filePaths)
        {
            var resource = await AnimationManager.load_resource(filePath);
            _resources[resource.resource_identifier] = resource;
            loadedCount++;
        }

        return loadedCount;
    }

    /// <summary>
    ///     根据资源标识获取资源
    /// </summary>
    /// <param name="resourceIdentifier">资源唯一标识</param>
    /// <returns>动画资源，未找到时返回 null</returns>
    public IAnimationResource? get_resource(string resourceIdentifier)
    {
        return _resources.TryGetValue(resourceIdentifier, out var resource) ? resource : null;
    }

    /// <summary>
    ///     移除指定资源
    /// </summary>
    /// <param name="resourceIdentifier">资源唯一标识</param>
    /// <returns>是否成功移除</returns>
    public bool remove_resource(string resourceIdentifier)
    {
        return _resources.Remove(resourceIdentifier);
    }

    /// <summary>
    ///     清空所有已加载资源
    /// </summary>
    public void clear_resources()
    {
        _resources.Clear();
    }
}