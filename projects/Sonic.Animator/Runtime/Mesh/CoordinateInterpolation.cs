using System;

namespace Animator.Runtime.Mesh;

/// <summary>
///     坐标插值计算工具
/// </summary>
public static class CoordinateInterpolation
{
    /// <summary>
    ///     线性插值两组顶点坐标
    /// </summary>
    /// <param name="source">源顶点坐标数组（交错 X/Y）</param>
    /// <param name="destination">目标顶点坐标数组（交错 X/Y）</param>
    /// <param name="interpolation">插值因子（0~1）</param>
    /// <param name="result">输出插值结果</param>
    public static void linear_interpolate(ReadOnlySpan<float> source, ReadOnlySpan<float> destination,
        float interpolation, Span<float> result)
    {
        for (var index = 0; index < source.Length; index++)
            result[index] = source[index] + (destination[index] - source[index]) * interpolation;
    }

    /// <summary>
    ///     贝塞尔曲线插值
    /// </summary>
    public static float bezier_interpolate(float start, float control1, float control2, float end, float interpolation)
    {
        var inverse = 1f - interpolation;
        return inverse * inverse * inverse * start
               + 3f * inverse * inverse * interpolation * control1
               + 3f * inverse * interpolation * interpolation * control2
               + interpolation * interpolation * interpolation * end;
    }
}