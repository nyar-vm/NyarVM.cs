using System;

namespace Animator.Runtime.Mesh;

/// <summary>
///     顶点偏移计算工具
/// </summary>
public static class VertexOffset
{
    /// <summary>
    ///     对顶点数组应用偏移量
    /// </summary>
    /// <param name="vertices">原始顶点坐标数组（交错 X/Y）</param>
    /// <param name="offsets">偏移量数组（交错 X/Y，长度与 vertices 相同）</param>
    /// <param name="weight">偏移权重（0~1）</param>
    public static void apply(ReadOnlySpan<float> vertices, ReadOnlySpan<float> offsets, float weight,
        Span<float> result)
    {
        for (var index = 0; index < vertices.Length; index++) result[index] = vertices[index] + offsets[index] * weight;
    }
}