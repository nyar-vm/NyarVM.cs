namespace Olympus.Athena.Index;

/// <summary>
///     向量近似最近邻搜索索引接口
/// </summary>
public interface IVectorIndex
{
    /// <summary>
    ///     索引中向量的维度
    /// </summary>
    int Dimension { get; }

    /// <summary>
    ///     批量构建向量索引
    /// </summary>
    /// <param name="vectors">向量集合，每个向量的维度必须与 <see cref="Dimension" /> 一致</param>
    /// <param name="ids">与向量一一对应的标识符集合</param>
    void Build(IReadOnlyList<float[]> vectors, IReadOnlyList<long> ids);

    /// <summary>
    ///     搜索与查询向量最相似的 Top-K 个向量
    /// </summary>
    /// <param name="query">查询向量</param>
    /// <param name="k">返回的最相似向量数量</param>
    /// <returns>包含匹配标识符和对应距离的元组</returns>
    (long[] ids, float[] distances) Search(float[] query, int k);
}