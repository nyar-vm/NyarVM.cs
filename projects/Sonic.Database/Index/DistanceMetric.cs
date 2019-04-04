namespace Olympus.Athena.Index;

/// <summary>
///     距离度量方式
/// </summary>
public enum DistanceMetric
{
    /// <summary>
    ///     L2 欧氏距离
    /// </summary>
    L2,

    /// <summary>
    ///     余弦距离（1 - 余弦相似度）
    /// </summary>
    Cosine
}