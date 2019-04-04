using System;
using System.Collections.Generic;
using Animator.Abstractions;

namespace Animator.Extensions;

/// <summary>
///     动画实例分组管理器
/// </summary>
public sealed class InstanceGroupManager
{
    private readonly Dictionary<string, List<IAnimationInstance>> _groups = new();

    /// <summary>
    ///     分组数量
    /// </summary>
    public int group_count => _groups.Count;

    /// <summary>
    ///     创建实例分组
    /// </summary>
    /// <param name="groupName">分组名称</param>
    public void create_group(string groupName)
    {
        if (!_groups.ContainsKey(groupName)) _groups[groupName] = [];
    }

    /// <summary>
    ///     将实例添加到指定分组
    /// </summary>
    /// <param name="groupName">分组名称</param>
    /// <param name="instance">动画实例</param>
    public void add_to_group(string groupName, IAnimationInstance instance)
    {
        if (!_groups.TryGetValue(groupName, out var group))
        {
            group = [];
            _groups[groupName] = group;
        }

        if (!group.Contains(instance)) group.Add(instance);
    }

    /// <summary>
    ///     从指定分组移除实例
    /// </summary>
    /// <param name="groupName">分组名称</param>
    /// <param name="instance">动画实例</param>
    /// <returns>是否成功移除</returns>
    public bool remove_from_group(string groupName, IAnimationInstance instance)
    {
        if (!_groups.TryGetValue(groupName, out var group)) return false;

        return group.Remove(instance);
    }

    /// <summary>
    ///     获取指定分组的所有实例
    /// </summary>
    /// <param name="groupName">分组名称</param>
    /// <returns>实例列表，分组不存在时返回空列表</returns>
    public IReadOnlyList<IAnimationInstance> get_group_instances(string groupName)
    {
        if (!_groups.TryGetValue(groupName, out var group)) return [];

        return group;
    }

    /// <summary>
    ///     批量更新指定分组内所有实例的帧
    /// </summary>
    /// <param name="groupName">分组名称</param>
    /// <param name="deltaTime">帧间隔时间</param>
    public void update_group_frame(string groupName, float deltaTime)
    {
        if (!_groups.TryGetValue(groupName, out var group)) return;

        foreach (var instance in group) instance.update_frame(deltaTime);
    }

    /// <summary>
    ///     批量播放指定分组内所有实例
    /// </summary>
    /// <param name="groupName">分组名称</param>
    /// <param name="animationName">动画名称</param>
    /// <param name="loopExecution">是否循环播放</param>
    public void play_group(string groupName, string animationName, bool loopExecution = true)
    {
        if (!_groups.TryGetValue(groupName, out var group)) return;

        foreach (var instance in group) instance.play(animationName, loopExecution);
    }

    /// <summary>
    ///     删除指定分组
    /// </summary>
    /// <param name="groupName">分组名称</param>
    /// <returns>是否成功删除</returns>
    public bool delete_group(string groupName)
    {
        return _groups.Remove(groupName);
    }
}