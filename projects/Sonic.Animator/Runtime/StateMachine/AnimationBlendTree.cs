using System.Collections.Generic;

namespace Animator.Runtime.StateMachine;

/// <summary>
///     动画混合树节点
/// </summary>
public sealed class AnimationBlendTree
{
    private readonly List<BlendChild> _children = [];

    public AnimationBlendTree(string parameterName)
    {
        parameter_name = parameterName;
    }

    /// <summary>混合参数名称</summary>
    public string parameter_name { get; }

    /// <summary>添加子动画节点</summary>
    public void add_child(string animationName, float threshold)
    {
        _children.Add(new BlendChild(animationName, threshold));
    }

    /// <summary>根据参数值计算各子动画的权重</summary>
    public void compute_weights(float parameterValue, List<BlendWeight> outputWeights)
    {
        outputWeights.Clear();

        if (_children.Count == 0) return;

        if (_children.Count == 1)
        {
            outputWeights.Add(new BlendWeight(_children[0].animation_name, 1f));
            return;
        }

        for (var index = 0; index < _children.Count - 1; index++)
        {
            var current = _children[index];
            var next = _children[index + 1];

            if (parameterValue <= next.threshold)
            {
                var range = next.threshold - current.threshold;
                var interpolation = range > 0f ? (parameterValue - current.threshold) / range : 0f;
                outputWeights.Add(new BlendWeight(current.animation_name, 1f - interpolation));
                outputWeights.Add(new BlendWeight(next.animation_name, interpolation));
                return;
            }
        }

        var last = _children[_children.Count - 1];
        outputWeights.Add(new BlendWeight(last.animation_name, 1f));
    }
}

/// <summary>
///     混合子节点
/// </summary>
public sealed class BlendChild
{
    public BlendChild(string animationName, float threshold)
    {
        animation_name = animationName;
        this.threshold = threshold;
    }

    /// <summary>动画名称</summary>
    public string animation_name { get; }

    /// <summary>阈值</summary>
    public float threshold { get; }
}

/// <summary>
///     混合权重
/// </summary>
public readonly struct BlendWeight
{
    /// <summary>动画名称</summary>
    public readonly string animation_name;

    /// <summary>权重值</summary>
    public readonly float weight;

    public BlendWeight(string animationName, float weight)
    {
        animation_name = animationName;
        this.weight = weight;
    }
}