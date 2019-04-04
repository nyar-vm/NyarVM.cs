namespace Core.AI;

/// <summary>
///     文本嵌入向量生成接口
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>
    ///     根据文本生成嵌入向量
    /// </summary>
    /// <param name="text">输入文本</param>
    /// <returns>嵌入向量</returns>
    float[] generate_embedding(string text);
}