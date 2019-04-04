using Core.AI;

namespace Std.AI;

/// <summary>
///     嵌入向量生成器默认实现，实现 IEmbeddingGenerator 接口
/// </summary>
public sealed class EmbeddingGenerator : IEmbeddingGenerator
{
    /// <summary>
    ///     嵌入维度
    /// </summary>
    private readonly int _dimensions;

    /// <summary>
    ///     初始化嵌入向量生成器
    /// </summary>
    /// <param name="dimensions">嵌入维度，默认为 128</param>
    public EmbeddingGenerator(int dimensions = 128)
    {
        _dimensions = dimensions;
    }

    /// <summary>
    ///     根据文本生成嵌入向量
    /// </summary>
    /// <param name="text">输入文本</param>
    /// <returns>嵌入向量</returns>
    public float[] generate_embedding(string text)
    {
        var embedding = new float[_dimensions];
        var hash = text.GetHashCode();
        for (var i = 0; i < _dimensions; i++) embedding[i] = (float)(System.Math.Sin(hash + i) * 0.5 + 0.5);
        return embedding;
    }
}