using System;

namespace Animator.Runtime.Timing;

/// <summary>
///     缓动函数库
/// </summary>
public static class EasingFunction
{
    /// <summary>线性缓动</summary>
    public static float linear(float interpolation)
    {
        return interpolation;
    }

    /// <summary>缓入（二次方）</summary>
    public static float ease_in(float interpolation)
    {
        return interpolation * interpolation;
    }

    /// <summary>缓出（二次方）</summary>
    public static float ease_out(float interpolation)
    {
        return interpolation * (2f - interpolation);
    }

    /// <summary>缓入缓出（二次方）</summary>
    public static float ease_in_out(float interpolation)
    {
        return interpolation < 0.5f
            ? 2f * interpolation * interpolation
            : -1f + (4f - 2f * interpolation) * interpolation;
    }

    /// <summary>弹跳缓动</summary>
    public static float bounce(float interpolation)
    {
        if (interpolation < 1f / 2.75f) return 7.5625f * interpolation * interpolation;

        if (interpolation < 2f / 2.75f)
        {
            var adjusted = interpolation - 1.5f / 2.75f;
            return 7.5625f * adjusted * adjusted + 0.75f;
        }

        if (interpolation < 2.5f / 2.75f)
        {
            var adjusted = interpolation - 2.25f / 2.75f;
            return 7.5625f * adjusted * adjusted + 0.9375f;
        }

        var final = interpolation - 2.625f / 2.75f;
        return 7.5625f * final * final + 0.984375f;
    }

    /// <summary>弹性缓动</summary>
    public static float elastic(float interpolation)
    {
        if (interpolation <= 0f) return 0f;

        if (interpolation >= 1f) return 1f;

        return (float)Math.Pow(2f, -10f * interpolation) *
            (float)Math.Sin((interpolation - 0.075f) * (2f * (float)Math.PI) / 0.3f) + 1f;
    }
}